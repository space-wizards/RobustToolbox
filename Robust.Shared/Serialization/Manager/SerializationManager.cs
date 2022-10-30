using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public sealed partial class SerializationManager : ISerializationManager
    {
        [IoC.Dependency] private readonly IReflectionManager _reflectionManager = default!;

        public IReflectionManager ReflectionManager => _reflectionManager;

        public const string LogCategory = "serialization";

        private bool _initializing;
        private bool _initialized;

        // Using CWT<,> here in case we ever want assembly unloading.
        private readonly ConditionalWeakTable<Type, DataDefinition> _dataDefinitions = new();
        private readonly HashSet<Type> _copyByRefRegistrations = new();

        public IDependencyCollection DependencyCollection { get; private set; } = default!;

        public void Initialize()
        {
            if (_initializing)
                throw new InvalidOperationException($"{nameof(SerializationManager)} is already being initialized.");

            if (_initialized)
                throw new InvalidOperationException($"{nameof(SerializationManager)} has already been initialized.");

            _initializing = true;

            DependencyCollection = IoCManager.Instance ?? throw new NullReferenceException();

            var flagsTypes = new ConcurrentBag<Type>();
            var constantsTypes = new ConcurrentBag<Type>();
            var typeSerializers = new ConcurrentBag<Type>();
            var meansDataDef = new ConcurrentBag<Type>();
            var meansDataRecord = new ConcurrentBag<Type>();
            var implicitDataDef = new ConcurrentBag<Type>();
            var implicitDataRecord = new ConcurrentBag<Type>();

            CollectAttributedTypes(flagsTypes, constantsTypes, typeSerializers, meansDataDef, meansDataRecord, implicitDataDef, implicitDataRecord);

            InitializeFlagsAndConstants(flagsTypes, constantsTypes);
            InitializeTypeSerializers(typeSerializers);

            // This is a bag, not a hash set.
            // Duplicates are fine since the CWT<,> won't re-run the constructor if it's already in there.
            var registrations = new ConcurrentBag<Type>();
            var records = new ConcurrentDictionary<Type, byte>();

            IEnumerable<Type> GetImplicitTypes(Type type)
            {
                // Inherited attributes don't work with interfaces.
                if (type.IsInterface)
                {
                    foreach (var child in _reflectionManager.GetAllChildren(type))
                    {
                        if (child.IsAbstract || child.IsGenericTypeDefinition)
                            continue;

                        yield return child;
                    }
                }
                else if (!type.IsAbstract && !type.IsGenericTypeDefinition)
                {
                    yield return type;
                }
            }

            foreach (var baseType in implicitDataDef)
            {
                foreach (var type in GetImplicitTypes(baseType))
                {
                    registrations.Add(type);
                }
            }

            foreach (var baseType in implicitDataRecord)
            {
                foreach (var type in GetImplicitTypes(baseType))
                {
                    records.TryAdd(type, 0);
                }
            }

            Parallel.ForEach(_reflectionManager.FindAllTypes(), type =>
            {
                if (meansDataDef.Any(type.IsDefined))
                    registrations.Add(type);

                if (type.IsDefined(typeof(DataRecordAttribute)) || meansDataRecord.Any(type.IsDefined))
                    records[type] = 0;
            });

            var sawmill = Logger.GetSawmill(LogCategory);

            Parallel.ForEach(registrations, type =>
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                {
                    sawmill.Debug(
                        $"Skipping registering data definition for type {type} since it is abstract or an interface");
                    return;
                }

                var isRecord = records.ContainsKey(type);
                if (!type.IsValueType && !isRecord && !type.HasParameterlessConstructor())
                {
                    sawmill.Debug(
                        $"Skipping registering data definition for type {type} since it has no parameterless ctor");
                    return;
                }

                _dataDefinitions.GetValue(type, t => CreateDataDefinition(t, DependencyCollection, isRecord));
            });

            var duplicateErrors = new StringBuilder();
            var invalidIncludes = new StringBuilder();

            //check for duplicates
            var dataDefs = _dataDefinitions.Select(x => x.Key).ToHashSet();
            var includeTree = new MultiRootInheritanceGraph<Type>();
            foreach (var (type, definition) in _dataDefinitions)
            {
                var invalidTypes = new List<string>();
                foreach (var includedField in definition.BaseFieldDefinitions.Where(x => x.Attribute is IncludeDataFieldAttribute
                         {
                             CustomTypeSerializer: null
                         }))
                {
                    if (!dataDefs.Contains(includedField.FieldType))
                    {
                        invalidTypes.Add(includedField.ToString());
                        continue;
                    }

                    includeTree.Add(includedField.FieldType, type);
                }

                if (invalidTypes.Count > 0)
                    invalidIncludes.Append($"{type}: [{string.Join(", ", invalidTypes)}]");

                if (definition.TryGetDuplicates(out var definitionDuplicates))
                {
                    duplicateErrors.Append($"{type}: [{string.Join(", ", definitionDuplicates)}]\n");
                }
            }

            if (duplicateErrors.Length > 0)
            {
                throw new ArgumentException($"Duplicate data field tags found in:\n{duplicateErrors}");
            }

            if (invalidIncludes.Length > 0)
            {
                throw new ArgumentException($"Invalid Types used for include fields:\n{invalidIncludes}");
            }

            _copyByRefRegistrations.Add(typeof(Type));

            _initialized = true;
            _initializing = false;
        }

        private void CollectAttributedTypes(
            ConcurrentBag<Type> flagsTypes,
            ConcurrentBag<Type> constantsTypes,
            ConcurrentBag<Type> typeSerializers,
            ConcurrentBag<Type> meansDataDef,
            ConcurrentBag<Type> meansDataRecord,
            ConcurrentBag<Type> implicitDataDef,
            ConcurrentBag<Type> implicitDataRecord)
        {
            // IsDefined is extremely slow. Great.
            Parallel.ForEach(_reflectionManager.FindAllTypes(), type =>
            {
                if (type.IsDefined(typeof(FlagsForAttribute), false))
                    flagsTypes.Add(type);

                if (type.IsDefined(typeof(ConstantsForAttribute), false))
                    constantsTypes.Add(type);

                if (type.IsDefined(typeof(TypeSerializerAttribute)))
                    typeSerializers.Add(type);

                if (type.IsDefined(typeof(MeansDataDefinitionAttribute)))
                    meansDataDef.Add(type);

                if (type.IsDefined(typeof(MeansDataRecordAttribute)))
                    meansDataRecord.Add(type);

                if (type.IsDefined(typeof(ImplicitDataDefinitionForInheritorsAttribute), true))
                    implicitDataDef.Add(type);

                if (type.IsDefined(typeof(ImplicitDataRecordAttribute), true))
                    implicitDataRecord.Add(type);

                if (type.IsDefined(typeof(CopyByRefAttribute)))
                    _copyByRefRegistrations.Add(type);
            });
        }

        private DataDefinition CreateDataDefinition(Type t, IDependencyCollection collection, bool isRecord)
        {
            return new(t, collection, GetOrCreateInstantiator(t, isRecord), isRecord);
        }

        public void Shutdown()
        {
            DependencyCollection = null!;

            _constantsMapping.Clear();
            _flagsMapping.Clear();

            _dataDefinitions.Clear();

            _copyByRefRegistrations.Clear();

            _highestFlagBit.Clear();

            _readers.Clear();

            _initialized = false;
        }

        public bool HasDataDefinition(Type type)
        {
            if (type.IsGenericTypeDefinition) throw new NotImplementedException($"Cannot yet check data definitions for generic types. ({type})");
            return _dataDefinitions.TryGetValue(type, out _);
        }

        internal DataDefinition? GetDefinition(Type type)
        {
            return _dataDefinitions.TryGetValue(type, out var dataDefinition)
                ? dataDefinition
                : null;
        }

        internal bool TryGetDefinition(Type type, [NotNullWhen(true)] out DataDefinition? dataDefinition)
        {
            dataDefinition = GetDefinition(type);
            return dataDefinition != null;
        }

        private static Type ResolveConcreteType(Type baseType, string typeName)
        {
            var reflection = IoCManager.Resolve<IReflectionManager>();
            var type = reflection.YamlTypeTagLookup(baseType, typeName);
            if (type == null)
            {
                throw new InvalidOperationException($"Type '{baseType}' is abstract, but could not find concrete type '{typeName}'.");
            }

            return type;
        }
    }
}
