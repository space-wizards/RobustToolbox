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
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Definition;
using Robust.Shared.Serialization.Manager.Result;
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

        public const string LogCategory = "serialization";

        private bool _initializing;
        private bool _initialized;

        // Using CWT<,> here in case we ever want assembly unloading.
        private static readonly ConditionalWeakTable<Type, DataDefinition> DataDefinitions = new();
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
            var implicitDataDefForInheritors = new ConcurrentBag<Type>();

            CollectAttributedTypes(flagsTypes, constantsTypes, typeSerializers, meansDataDef, implicitDataDefForInheritors);

            InitializeFlagsAndConstants(flagsTypes, constantsTypes);
            InitializeTypeSerializers(typeSerializers);

            // This is a bag, not a hash set.
            // Duplicates are fine since the CWT<,> won't re-run the constructor if it's already in there.
            var registrations = new ConcurrentBag<Type>();

            foreach (var baseType in implicitDataDefForInheritors)
            {
                // Inherited attributes don't work with interfaces.
                if (baseType.IsInterface)
                {
                    foreach (var child in _reflectionManager.GetAllChildren(baseType))
                    {
                        if (child.IsAbstract || child.IsGenericTypeDefinition)
                            continue;

                        registrations.Add(child);
                    }
                }
                else if (!baseType.IsAbstract && !baseType.IsGenericTypeDefinition)
                {
                    registrations.Add(baseType);
                }
            }

            Parallel.ForEach(_reflectionManager.FindAllTypes(), type =>
            {
                foreach (var meansDataDefAttr in meansDataDef)
                {
                    if (type.IsDefined(meansDataDefAttr))
                        registrations.Add(type);
                }
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

                if (!type.IsValueType && type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(m => m.GetParameters().Length == 0) == null)
                {
                    sawmill.Debug(
                        $"Skipping registering data definition for type {type} since it has no parameterless ctor");
                    return;
                }

                DataDefinitions.GetValue(type, CreateDefinitionCallback);
            });

            var error = new StringBuilder();

            foreach (var (type, definition) in DataDefinitions)
            {
                if (definition.TryGetDuplicates(out var definitionDuplicates))
                {
                    error.Append($"{type}: [{string.Join(", ", definitionDuplicates)}]\n");
                }
            }

            if (error.Length > 0)
            {
                throw new ArgumentException($"Duplicate data field tags found in:\n{error}");
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
            ConcurrentBag<Type> implicitDataDefForInheritors)
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

                if (type.IsDefined(typeof(ImplicitDataDefinitionForInheritorsAttribute), true))
                    implicitDataDefForInheritors.Add(type);

                if (type.IsDefined(typeof(CopyByRefAttribute)))
                    _copyByRefRegistrations.Add(type);
            });
        }

        private static readonly ConditionalWeakTable<Type, DataDefinition>.CreateValueCallback
            CreateDefinitionCallback = CreateDataDefinition;

        private static DataDefinition CreateDataDefinition(Type t)
        {
            return new(t);
        }

        public void Shutdown()
        {
            DependencyCollection = null!;

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

            DataDefinitions.Clear();

            _copyByRefRegistrations.Clear();

            _highestFlagBit.Clear();

            _readers.Clear();

            _initialized = false;
        }

        public bool HasDataDefinition(Type type)
        {
            if (type.IsGenericTypeDefinition) throw new NotImplementedException($"Cannot yet check data definitions for generic types. ({type})");
            return DataDefinitions.TryGetValue(type, out _);
        }

        public ValidationNode ValidateNode(Type type, DataNode node, ISerializationContext? context = null)
        {
            var underlyingType = type.EnsureNotNullableType();

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
                    underlyingType = ResolveConcreteType(underlyingType, typeString);
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

        public DeserializationResult CreateDataDefinition<T>(DeserializedFieldEntry[] fields, bool skipHook = false)
            where T : notnull, new()
        {
            var obj = new T();
            return PopulateDataDefinition(obj, new DeserializedDefinition<T>(obj, fields), skipHook);
        }

        public DeserializationResult PopulateDataDefinition<T>(T obj, DeserializedDefinition<T> definition, bool skipHook = false)
            where T : notnull, new()
        {
            return PopulateDataDefinition(obj, (IDeserializedDefinition) definition, skipHook);
        }

        public DeserializationResult PopulateDataDefinition(object obj, IDeserializedDefinition definition, bool skipHook = false)
        {
            if (!TryGetDefinition(obj.GetType(), out var dataDefinition))
                throw new ArgumentException($"Provided Type is not a data definition ({obj.GetType()})");

            if (obj is IPopulateDefaultValues populateDefaultValues)
            {
                populateDefaultValues.PopulateDefaultValues();
            }

            var res = dataDefinition.Populate(obj, definition.Mapping);

            if (!skipHook && res.RawValue is ISerializationHooks serializationHooksAfter)
            {
                serializationHooksAfter.AfterDeserialization();
            }

            return res;
        }

        internal DataDefinition? GetDefinition(Type type)
        {
            return DataDefinitions.TryGetValue(type, out var dataDefinition)
                ? dataDefinition
                : null;
        }

        private bool TryGetDefinition(Type type, [NotNullWhen(true)] out DataDefinition? dataDefinition)
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

            if (value == null) return new MappingDataNode();

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

            if (value is ISerializationHooks serHook)
                serHook.BeforeSerialization();

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
            mapping = mapping.Merge(newMapping);

            return mapping;
        }

        public DataNode WriteWithTypeSerializer(Type type, Type serializer, object? value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            // TODO Serialization: just return null
            if (type.IsNullable() && value == null) return new MappingDataNode();

            return WriteWithSerializerRaw(type, serializer, value!, context, alwaysWrite);
        }

        private object? CopyToTarget(object? source, object? target, ISerializationContext? context = null, bool skipHook = false)
        {
            if (source == null || target == null)
            {
                return source;
            }

            var sourceType = source.GetType();
            var targetType = target.GetType();

            if (sourceType.IsValueType && targetType.IsValueType)
            {
                return source;
            }

            if (sourceType.IsValueType != targetType.IsValueType)
            {
                throw new InvalidOperationException(
                    $"Source and target do not match. Source ({sourceType}) is value type? {sourceType.IsValueType}. Target ({targetType}) is value type? {targetType.IsValueType}");
            }

            if (sourceType.IsArray && targetType.IsArray)
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
                    newArray.SetValue(CreateCopy(sourceArray.GetValue(i), context, skipHook), i);
                }

                return newArray;
            }

            if (sourceType.IsArray != targetType.IsArray)
            {
                throw new InvalidOperationException(
                    $"Source and target do not match. Source ({sourceType}) is array type? {sourceType.IsArray}. Target ({targetType}) is array type? {targetType.IsArray}");
            }

            var commonType = TypeHelpers.SelectCommonType(sourceType, targetType);
            if (commonType == null)
            {
                throw new InvalidOperationException("Could not find common type in Copy!");
            }

            if (_copyByRefRegistrations.Contains(commonType) || commonType.IsEnum)
            {
                return source;
            }

            if (TryCopyRaw(commonType, source, ref target, skipHook, context))
            {
                return target;
            }

            if (target is IPopulateDefaultValues populateDefaultValues)
            {
                populateDefaultValues.PopulateDefaultValues();
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

            return target;
        }

        [MustUseReturnValue]
        public object? Copy(object? source, object? target, ISerializationContext? context = null, bool skipHook = false)
        {
            return CopyToTarget(source, target, context, skipHook);
        }

        [MustUseReturnValue]
        public T? Copy<T>(T? source, T? target, ISerializationContext? context = null, bool skipHook = false)
        {
            var copy = CopyToTarget(source, target, context, skipHook);

            return copy == null ? default : (T?) copy;
        }

        [MustUseReturnValue]
        public object? CopyWithTypeSerializer(Type typeSerializer, object? source, object? target,
            ISerializationContext? context = null, bool skipHook = false)
        {
            if (source == null || target == null) return source;

            return CopyWithSerializerRaw(typeSerializer, source, ref target, skipHook, context);
        }

        private object? CreateCopyInternal(Type type, object? source, ISerializationContext? context = null, bool skipHook = false)
        {
            if (source == null ||
                type.IsPrimitive ||
                type.IsEnum ||
                source is string ||
                _copyByRefRegistrations.Contains(type))
            {
                return source;
            }

            var target = Activator.CreateInstance(source.GetType())!;
            return Copy(source, target, context, skipHook);
        }

        public object? CreateCopy(object? source, ISerializationContext? context = null, bool skipHook = false)
        {
            if (source == null) return null;
            return CreateCopyInternal(source.GetType(), source, context, skipHook);
        }

        public T? CreateCopy<T>(T? source, ISerializationContext? context = null, bool skipHook = false)
        {
            var copy = CreateCopyInternal(typeof(T), source, context, skipHook);

            if (copy == null)
            {
                return default;
            }

            return (T?) copy;
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
