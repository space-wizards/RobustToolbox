using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using YamlDotNet.Core.Tokens;

namespace Robust.Shared.Serialization.Manager
{
    public partial class SerializationManager : ISerializationManager
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        private readonly Dictionary<Type, SerializationDataDefinition> _dataDefinitions = new();
        private readonly List<Type> _copyByRefRegistrations = new();

        public void Initialize()
        {
            InitializeFlagsAndConstants();
            InitializeTypeSerializers();

            //var registrations = _reflectionManager.FindTypesWithAttribute<MeansDataDefinition>().ToHashSet();
            var registrations = new HashSet<Type>();

            foreach (var baseType in _reflectionManager.FindTypesWithAttribute<ImplicitDataDefinitionForInheritorsAttribute>())
            {
                registrations.Add(baseType);
                foreach (var child in _reflectionManager.GetAllChildren(baseType))
                {
                    if (child.IsAbstract || child.IsInterface) continue;
                    registrations.Add(child);
                }
            }

            foreach (var meansAttr in _reflectionManager.FindTypesWithAttribute<MeansDataDefinition>())
            {
                foreach (var type in _reflectionManager.FindTypesWithAttribute(meansAttr))
                {
                    registrations.Add(type);
                }
            }

            foreach (var type in registrations)
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    Logger.Warning($"Skipping registering data definition for type {type} since it is abstract or an interface");
                    continue;
                }

                if (!type.IsValueType && type.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(m => m.GetParameters().Length == 0) == null)
                {
                    Logger.Warning($"Skipping registering data definition for type {type} since it has no parameterless ctor");
                    continue;
                }

                _dataDefinitions.Add(type, new SerializationDataDefinition(type));
            }

            var error = new StringBuilder();

            foreach (var (type, definition) in _dataDefinitions)
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

            foreach (var type in _reflectionManager.FindTypesWithAttribute<CopyByRefAttribute>())
            {
                _copyByRefRegistrations.Add(type);
            }
        }

        public bool HasDataDefinition(Type type)
        {
            if (type.IsGenericTypeDefinition) throw new NotImplementedException($"Cannot yet check data definitions for generic types. ({type})");
            return _dataDefinitions.ContainsKey(type);
        }

        public DeserializationResult CreateDataDefinition<T>(DeserializedFieldEntry[] fields) where T : notnull, new()
        {
            var obj = new T();
            return PopulateDataDefinition(obj, new DeserializedDefinition<T>(obj, fields));
        }

        public DeserializationResult PopulateDataDefinition<T>(T obj, DeserializedDefinition<T> definition) where T : notnull, new()
        {
            return PopulateDataDefinition(obj, (IDeserializedDefinition) definition);
        }

        public DeserializationResult PopulateDataDefinition(object obj, IDeserializedDefinition deserializationResult)
        {
            if (!TryGetDataDefinition(obj.GetType(), out var dataDefinition))
                throw new ArgumentException($"Provided Type is not a data definition ({obj.GetType()})");

            if(obj is ISerializationHooks serializationHooksBefore) serializationHooksBefore.BeforeSerialization();
            var res = dataDefinition.InvokePopulateDelegate(obj, deserializationResult.Mapping);
            if(res.RawValue is ISerializationHooks serializationHooksAfter) serializationHooksAfter.AfterDeserialization();
            return res;
        }

        private SerializationDataDefinition? GetDataDefinition(Type type)
        {
            if (_dataDefinitions.TryGetValue(type, out var dataDefinition)) return dataDefinition;

            return null;
        }

        private bool TryGetDataDefinition(Type type, [NotNullWhen(true)] out SerializationDataDefinition? dataDefinition)
        {
            dataDefinition = GetDataDefinition(type);
            return dataDefinition != null;
        }

        public DeserializationResult Read(Type type, DataNode node, ISerializationContext? context = null)
        {
            var underlyingType = type.EnsureNotNullableType();

            // val primitives
            if (underlyingType.IsPrimitive || underlyingType == typeof(decimal))
            {
                if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
                var foo = TypeDescriptor.GetConverter(type);
                return DeserializationResult.Value(foo.ConvertFromInvariantString(valueDataNode.Value));
            }

            // array
            if (type.IsArray)
            {
                if (node is not SequenceDataNode sequenceDataNode) throw new InvalidNodeTypeException();
                var newArray = (Array) Activator.CreateInstance(type, sequenceDataNode.Sequence.Count)!;
                var results = new List<DeserializationResult>();

                var idx = 0;
                foreach (var entryNode in sequenceDataNode.Sequence)
                {
                    var value = Read(type.GetElementType()!, entryNode, context);
                    results.Add(value);
                    newArray.SetValue(value.RawValue, idx++);
                }

                return new DeserializedArray(newArray, results);
            }

            if (underlyingType.IsEnum)
            {
                return DeserializationResult.Value(node switch
                {
                    ValueDataNode valueNode => Enum.Parse(underlyingType, valueNode.Value, true),
                    SequenceDataNode sequenceNode => Enum.Parse(underlyingType, string.Join(", ", sequenceNode.Sequence), true),
                    _ => throw new InvalidNodeTypeException($"Cannot serialize node as {underlyingType}, unsupported node type {node.GetType()}")
                });
            }

            if (node.Tag?.StartsWith("!type:") == true)
            {
                var typeString = node.Tag.Substring(6);
                underlyingType = ResolveConcreteType(underlyingType, typeString);
            }

            if (TryReadWithTypeSerializers(underlyingType, node, out var serializedObj, context))
            {
                return serializedObj;
            }

            if (typeof(ISelfSerialize).IsAssignableFrom(underlyingType))
            {
                if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();

                var selfSerObj = (ISelfSerialize) Activator.CreateInstance(underlyingType)!;
                selfSerObj.Deserialize(valueDataNode.Value);

                return DeserializationResult.Value(selfSerObj);
            }

            //if (node is not MappingDataNode mappingDataNode) throw new InvalidNodeTypeException();

            if (underlyingType.IsInterface || underlyingType.IsAbstract)
            {
                 throw new InvalidOperationException($"Unable to create an instance of an interface/abstract type. Type: {underlyingType}");
            }

            var obj = Activator.CreateInstance(underlyingType)!;

            if (obj is IPopulateDefaultValues populateDefaultValues)
            {
                populateDefaultValues.PopulateDefaultValues();
            }

            if (!TryGetDataDefinition(underlyingType, out var dataDef))
            {
                throw new InvalidOperationException($"No data definition found for type {type} with nodetype {node.GetType()} when reading");
            }

            if (node is not MappingDataNode mappingDataNode)
            {
                if(node is not ValueDataNode emptyValueDataNode || emptyValueDataNode.Value != "")
                    throw new ArgumentException($"No mapping node provided for type {type}");
                mappingDataNode = new MappingDataNode(); //if we get an emptyValueDataNode we just use an empty mapping
            }

            var res = dataDef.InvokePopulateDelegate(obj, mappingDataNode, this, context);

            if (res.RawValue is ISerializationHooks serHooks)
            {
                serHooks.AfterDeserialization();
            }

            return res;
        }

        public object? ReadValue(Type type, DataNode node, ISerializationContext? context = null)
        {
            return Read(type, node, context).RawValue;
        }

        public T? ReadValue<T>(Type type, DataNode node, ISerializationContext? context = null)
        {
            var value = Read(type, node, context);

            if (value.RawValue == null)
            {
                return default;
            }

            return (T) value.RawValue;
        }

        public T? ReadValue<T>(DataNode node, ISerializationContext? context = null)
        {
            return ReadValue<T>(typeof(T), node, context);
        }

        public DataNode WriteValue<T>(T value, bool alwaysWrite = false,
            ISerializationContext? context = null) where T : notnull
        {
            return WriteValue(typeof(T), value, alwaysWrite, context);
        }

        public DataNode WriteValue(Type type, object value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            //todo paul
            //var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (value == null) return new MappingDataNode();

            if (type.IsPrimitive || type.IsEnum || type == typeof(decimal))
            {
                // All primitives and enums implement IConvertible.
                // Need it for the culture overload.
                var convertible = (IConvertible) value;
                return new ValueDataNode(convertible.ToString(CultureInfo.InvariantCulture));
            }

            // array
            if (type.IsArray)
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

            if (TryWriteWithTypeSerializers(type, value, out var node, alwaysWrite, context))
            {
                return node;
            }

            if (typeof(ISelfSerialize).IsAssignableFrom(type))
            {
                var selfSerObj = (ISelfSerialize)value;
                return new ValueDataNode(selfSerObj.Serialize());
            }

            var currentType = type;
            var mapping = new MappingDataNode();
            if (type != value.GetType() && (type.IsAbstract || type.IsInterface))
            {
                mapping.Tag = $"!type:{value.GetType().Name}";
                currentType = value.GetType();
            }

            if (!TryGetDataDefinition(currentType, out var dataDef))
            {
                throw new InvalidOperationException($"No data definition found for type {type} when writing");
            }

            if (dataDef.CanCallWith(value) != true)
            {
                throw new ArgumentException($"Supplied value does not fit with data definition of {type}.");
            }

            var newMapping = dataDef.InvokeSerializeDelegate(value, this, context, alwaysWrite);
            mapping = mapping.Merge(newMapping);

            return mapping.Children.Count == 0 ? new ValueDataNode(""){Tag = mapping.Tag} : mapping;
        }

        private object? CopyToTarget(object? source, object? target, ISerializationContext? context = null)
        {
            if (source == null || target == null)
            {
                return source;
            }

            var sourceType = source.GetType();
            var targetType = target.GetType();

            if (targetType.IsPrimitive && sourceType.IsPrimitive)
            {
                //todo does this work
                //i think it does
                //todo validate we can assign source
                return source;
            }

            if (target.GetType().IsPrimitive != source.GetType().IsPrimitive)
                throw new InvalidOperationException(
                    $"Source and target do not match. Source ({sourceType}) is primitive type: {sourceType.IsPrimitive}. Target ({targetType}) is primitive type: {targetType.IsPrimitive}");

            var commonType = TypeHelpers.SelectCommonType(source.GetType(), target.GetType());
            if(commonType == null)
            {
                throw new InvalidOperationException("Could not find common type in Copy!");
            }

            if (_copyByRefRegistrations.Contains(commonType) || commonType.IsEnum)
            {
                return source;
            }

            if (TryCopyWithTypeCopier(commonType, source, ref target, context))
            {
                return target;
            }

            if (!TryGetDataDefinition(commonType, out var dataDef))
            {
                throw new InvalidOperationException($"No data definition found for type {commonType} when copying");
            }

            target = dataDef.InvokeCopyDelegate(source, target, this, context);

            return target;
        }

        public object? Copy(object? source, object? target, ISerializationContext? context = null)
        {
            return CopyToTarget(source, target, context);
        }

        public T? Copy<T>(object? source, T? target, ISerializationContext? context = null)
        {
            var copy = CopyToTarget(source, target, context);

            if (copy == null)
            {
                return default;
            }

            return (T?) copy;
        }

        private object? CreateCopyInternal(object? source, ISerializationContext? context = null)
        {
            if (source == null) return source;
            //todo paul checks here
            var target = Activator.CreateInstance(source.GetType())!;
            return Copy(source, target, context);
        }

        public object? CreateCopy(object? source, ISerializationContext? context = null)
        {
            return CreateCopyInternal(source, context);
        }

        public T? CreateCopy<T>(T? source, ISerializationContext? context = null)
        {
            var copy = CreateCopyInternal(source, context);

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
