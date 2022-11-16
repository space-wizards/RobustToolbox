using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
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

        [field: IoC.Dependency]
        public IDependencyCollection DependencyCollection { get; } = default!;

        public void Initialize()
        {
            if (_initializing)
                throw new InvalidOperationException($"{nameof(SerializationManager)} is already being initialized.");

            if (_initialized)
                throw new InvalidOperationException($"{nameof(SerializationManager)} has already been initialized.");

            _initializing = true;

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
            _constantsMapping.Clear();
            _flagsMapping.Clear();

            _genericWriterTypes.Clear();
            _genericReaderTypes.Clear();
            _genericCopierTypes.Clear();
            _genericValidatorTypes.Clear();

            _typeWriters.Clear();
            _typeReaders.Clear();
            _typeCopiers.Clear();
            _typeValidators.Clear();

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

        public ValidationNode ValidateNode(Type type, DataNode node, ISerializationContext? context = null)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);

            if (underlyingType != null) // implies that type was nullable
            {
                if (IsNull(node))
                    return new ValidatedValueNode(node);
            }
            else
            {
                underlyingType = type;
            }

            if (underlyingType.IsArray)
            {
                if (node is not SequenceDataNode sequenceDataNode) return new ErrorNode(node, "Invalid nodetype for array.", true);
                var elementType = underlyingType.GetElementType();
                if (elementType == null)
                    throw new ArgumentException($"Failed to get elementtype of arraytype {underlyingType}", nameof(underlyingType));
                var validatedList = new List<ValidationNode>();
                foreach (var dataNode in sequenceDataNode.Sequence)
                {
                    validatedList.Add(ValidateNode(elementType, dataNode, context));
                }

                return new ValidatedSequenceNode(validatedList);
            }

            if (underlyingType.IsEnum)
            {
                var enumName = node switch
                {
                    ValueDataNode valueNode => valueNode.Value,
                    SequenceDataNode sequenceNode => string.Join(", ", sequenceNode.Sequence),
                    _ => null
                };

                if (enumName == null)
                {
                    return new ErrorNode(node, $"Invalid node type {node.GetType().Name} for enum {underlyingType}.");
                }

                if (!Enum.TryParse(underlyingType, enumName, true, out var enumValue))
                {
                    return new ErrorNode(node, $"{enumValue} is not a valid enum value of type {underlyingType}", false);
                }

                return new ValidatedValueNode(node);
            }

            if (node.Tag?.StartsWith("!type:") == true)
            {
                var typeString = node.Tag.Substring(6);
                try
                {
                    underlyingType = ResolveConcreteType(underlyingType, typeString, _reflectionManager);
                }
                catch (InvalidOperationException)
                {
                    return new ErrorNode(node, $"Failed to resolve !type tag: {typeString}", false);
                }
            }

            if (TryValidateWithTypeValidator(underlyingType, node, DependencyCollection, context, out var valid)) return valid;

            if (typeof(ISelfSerialize).IsAssignableFrom(underlyingType))
                return node is ValueDataNode valueDataNode ? new ValidatedValueNode(valueDataNode) : new ErrorNode(node, "Invalid nodetype for ISelfSerialize", true);

            if (TryGetDefinition(underlyingType, out var dataDefinition))
            {
                return node switch
                {
                    ValueDataNode valueDataNode => valueDataNode.Value == "" ? new ValidatedValueNode(valueDataNode) : new ErrorNode(node, "Invalid nodetype for Datadefinition", false),
                    MappingDataNode mappingDataNode => dataDefinition.Validate(this, mappingDataNode, context),
                    _ => new ErrorNode(node, "Invalid nodetype for Datadefinition", true)
                };
            }

            return new ErrorNode(node, "Failed to read node.", false);
        }

        public ValidationNode ValidateNode<T>(DataNode node, ISerializationContext? context = null)
        {
            return ValidateNode(typeof(T), node, context);
        }

        public ValidationNode ValidateNodeWith(Type type, Type typeSerializer, DataNode node,
            ISerializationContext? context = null)
        {
            var method =
                typeof(SerializationManager).GetRuntimeMethods().First(m => m.Name == nameof(ValidateWithSerializer))!.MakeGenericMethod(
                    type, node.GetType(), typeSerializer);
            return (ValidationNode)method.Invoke(this, new object?[] {node, context})!;
        }

        public ValidationNode ValidateNodeWith<TType, TSerializer, TNode>(TNode node,
            ISerializationContext? context = null)
            where TSerializer : ITypeValidator<TType, TNode>
            where TNode: DataNode
        {
            return ValidateNodeWith(typeof(TType), typeof(TSerializer), node, context);
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

        public DataNode WriteValue<T>(T value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return WriteValue(typeof(T), value, alwaysWrite, context);
        }

        public DataNode WriteValue(Type type, object? value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (value == null) return new ValueDataNode("null");

            if (underlyingType.IsEnum)
            {
                // Enums implement IConvertible.
                // Need it for the culture overload.
                var convertible = (IConvertible) value;
                return new ValueDataNode(convertible.ToString(CultureInfo.InvariantCulture));
            }

            if (underlyingType.IsArray)
            {
                var sequenceNode = new SequenceDataNode();
                var array = (Array) value;

                foreach (var val in array)
                {
                    var serializedVal = WriteValue(val.GetType(), val, alwaysWrite, context);
                    sequenceNode.Add(serializedVal);
                }

                return sequenceNode;
            }

            if (TryWriteRaw(underlyingType, value, out var node, alwaysWrite, context))
            {
                return node;
            }

            if (typeof(ISelfSerialize).IsAssignableFrom(underlyingType))
            {
                var selfSerObj = (ISelfSerialize)value;
                return new ValueDataNode(selfSerObj.Serialize());
            }

            var currentType = underlyingType;
            var mapping = new MappingDataNode();
            if (underlyingType.IsAbstract || underlyingType.IsInterface)
            {
                mapping.Tag = $"!type:{value.GetType().Name}";
                currentType = value.GetType();
            }

            if (!TryGetDefinition(currentType, out var dataDef))
            {
                throw new InvalidOperationException($"No data definition found for type {type} when writing");
            }

            if (dataDef.CanCallWith(value) != true)
            {
                throw new ArgumentException($"Supplied value does not fit with data definition of {type}.");
            }

            var newMapping = dataDef.Serialize(value, this, context, alwaysWrite);
            mapping.Insert(newMapping);

            return mapping;
        }

        public DataNode WriteWithTypeSerializer(Type type, Type serializer, object? value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            // TODO Serialization: just return null
            if (type.IsNullable() && value == null) return new MappingDataNode();

            return WriteWithSerializerRaw(type, serializer, value!, context, alwaysWrite);
        }

        private void CopyToTarget(object source, ref object target, ISerializationContext? context = null, bool skipHook = false)
        {
            if (!TypeHelpers.TrySelectCommonType(source.GetType(), target.GetType(), out var commonType))
            {
                throw new InvalidOperationException($"Could not find common type in Copy for types {source.GetType()} and {target.GetType()}!");
            }

            if (ShouldReturnSource(commonType))
            {
                target = source;
                return;
            }

            if (commonType.IsArray)
            {
                var sourceArray = (Array) source;
                var targetArray = (Array) target;

                Array newArray;
                if(sourceArray.Length == targetArray.Length)
                {
                    newArray = targetArray;
                }
                else
                {
                    newArray = (Array) Activator.CreateInstance(sourceArray.GetType(), sourceArray.Length)!;
                }

                for (var i = 0; i < sourceArray.Length; i++)
                {
                    newArray.SetValue(Copy(sourceArray.GetValue(i), context, skipHook), i);
                }

                target = newArray;
                return;
            }

            if (TryCopyRaw(commonType, source, ref target, skipHook, context))
            {
                return;
            }

            if (!TryGetDefinition(commonType, out var dataDef))
            {
                throw new InvalidOperationException($"No data definition found for type {commonType} when copying");
            }

            target = dataDef.Copy(source, target, this, context);

            if (!skipHook && target is ISerializationHooks afterHooks)
            {
                afterHooks.AfterDeserialization();
            }
        }

        public void Copy(object? source, ref object? target, ISerializationContext? context = null, bool skipHook = false)
        {
            if (target == null || source == null)
            {
                target = Copy(source, context, skipHook);
            }
            else
            {
                CopyToTarget(source, ref target, context, skipHook);
            }
        }

        public void Copy<T>(T source, ref T target, ISerializationContext? context = null, bool skipHook = false)
        {
            var temp = (object?)target;
            Copy(source, ref temp, context, skipHook);
            target = (T)temp!;
        }

        [MustUseReturnValue]
        public object? CopyWithTypeSerializer(Type typeSerializer, object? source, object? target,
            ISerializationContext? context = null, bool skipHook = false)
        {
            if (source == null)
                return null;

            // TODO should this respect _copyByRefRegistrations? Or should the type serializer be allowed to override this?

            if (target == null)
            {
                // TODO allow type serializers to copy into null (or make them provide a parameterless constructor & copy into a
                // new instance). For now, this needs to assume that this is a value type or that this has a parameterless ctor

                var type = source.GetType();

                if (type.IsPrimitive ||
                    type.IsEnum ||
                    source is string)
                {
                    target = source;
                }
                else
                {
                    target = Activator.CreateInstance(type, true)!;
                }
            }

            return CopyWithSerializerRaw(typeSerializer, source, ref target, skipHook, context);
        }

        private object CreateCopyInternal(Type type, object source, ISerializationContext? context = null, bool skipHook = false)
        {
            if (ShouldReturnSource(type))
            {
                return source;
            }

            var target = Activator.CreateInstance(source.GetType())!;
            CopyToTarget(source, ref target, context, skipHook);
            return target;
        }

        public object? Copy(object? source, ISerializationContext? context = null, bool skipHook = false)
        {
            if (source == null) return null;
            return CreateCopyInternal(source.GetType(), source, context, skipHook);
        }

        public T Copy<T>(T source, ISerializationContext? context = null, bool skipHook = false)
        {
            return (T)Copy((object?)source, context, skipHook)!;
        }

        private bool ShouldReturnSource(Type type)
        {
            return type.IsPrimitive ||
                   type.IsEnum ||
                   type == typeof(string) ||
                   _copyByRefRegistrations.Contains(type) ||
                   type.IsValueType;
        }

        private static Type ResolveConcreteType(Type baseType, string typeName, IReflectionManager reflection)
        {
            var type = reflection.YamlTypeTagLookup(baseType, typeName);
            if (type == null)
            {
                throw new InvalidOperationException($"Type '{baseType}' is abstract, but could not find concrete type '{typeName}'.");
            }

            return type;
        }
    }
}
