using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Manager
{
    public partial class Serv3Manager : IServ3Manager
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        private Dictionary<Type, SerializationDataDefinition> _dataDefinitions = new();

        private List<Type> _copyByRefRegistrations = new();

        public void Initialize()
        {
            InitializeFlagsAndConstants();
            InitializeTypeSerializers();

            //var registrations = _reflectionManager.FindTypesWithAttribute<MeansDataDefinition>().ToHashSet();
            var registrations = new HashSet<Type>();

            foreach (var baseType in _reflectionManager.FindTypesWithAttribute<ImplicitDataDefinitionForInheritorsAttribute>())
            {
                foreach (var child in _reflectionManager.GetAllChildren(baseType))
                {
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
                _dataDefinitions.Add(type, new SerializationDataDefinition(type));
            }

            foreach (var type in _reflectionManager.FindTypesWithAttribute<CopyByRefAttribute>())
            {
                _copyByRefRegistrations.Add(type);
            }
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

        public T ReadValue<T>(DataNode node, ISerializationContext? context = null)
        {
            return (T)ReadValue(typeof(T), node, context);
        }

        public object ReadValue(Type type, DataNode node, ISerializationContext? context = null)
        {
            var underlyingType = type.EnsureNotNullableType();

            // val primitives
            if (underlyingType.IsPrimitive || underlyingType == typeof(decimal))
            {
                if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
                var foo = TypeDescriptor.GetConverter(type);
                return foo.ConvertFromInvariantString(valueDataNode.Value);
            }

            // array
            if (type.IsArray)
            {
                if (node is not SequenceDataNode sequenceDataNode) throw new InvalidNodeTypeException();
                var newArray = (Array)Activator.CreateInstance(type, sequenceDataNode.Sequence.Count)!;

                var idx = 0;
                foreach (var entryNode in sequenceDataNode.Sequence)
                {
                    var value = ReadValue(type.GetElementType()!, entryNode, context);
                    newArray.SetValue(value, idx++);
                }

                return newArray;
            }

            if (underlyingType.IsEnum)
            {
                if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
                return Enum.Parse(underlyingType, valueDataNode.Value);
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
                return selfSerObj;
            }

            //if (node is not MappingDataNode mappingDataNode) throw new InvalidNodeTypeException();

            var currentType = underlyingType;
            var obj = Activator.CreateInstance(underlyingType)!;

            if(obj is IPopulateDefaultValues populateDefaultValues)
                populateDefaultValues.PopulateDefaultValues();

            if(node is MappingDataNode mappingDataNode)
            {
                while (currentType != null)
                {
                    if(TryGetDataDefinition(currentType, out var dataDef))
                        obj = dataDef.InvokePopulateDelegate(obj, mappingDataNode, this, context);

                    currentType = currentType.BaseType;
                }
            }

            if (obj is ISerializationHooks serHooks)
                serHooks.AfterDeserialization();

            return obj;
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

            while (currentType != null)
            {
                if (TryGetDataDefinition(currentType, out var dataDef))
                {
                    if (dataDef.CanCallWith(value) != true)
                        throw new ArgumentException($"Supplied parameter does not fit with datadefinition of {type}.", nameof(value));
                    var newMapping = dataDef.InvokeSerializeDelegate(value, this, context, alwaysWrite);
                    mapping = mapping.Merge(newMapping);
                }
                currentType = currentType.BaseType;
            }

            return mapping.Children.Count == 0 ? new ValueDataNode(""){Tag = mapping.Tag} : mapping;
        }

        public object? Copy(object? source, object? target)
        {
            if (source == null || target == null)
            {
                return source;
            }

            if (target.GetType().IsPrimitive == source.GetType().IsPrimitive)
            {
                //todo does this work
                //todo validate we can assign source
                return source;
            }

            if (target.GetType().IsPrimitive != source.GetType().IsPrimitive)
                throw new InvalidOperationException();

            var commonType = TypeHelpers.FindCommonType(source.GetType(), target.GetType());
            if(commonType == null)
            {
                throw new InvalidOperationException("Could not find common type in PushInheritance!");
            }

            if (_copyByRefRegistrations.Contains(commonType))
            {
                return source;
            }


            while (commonType != null)
            {
                if(TryGetDataDefinition(commonType, out var dataDef))
                    target = dataDef.InvokeCopyDelegate(source, target, this);
                commonType = commonType.BaseType;
            }

            return target;
        }

        public object? CreateCopy(object? source)
        {
            if (source == null) return source;
            //todo paul checks here
            var target = Activator.CreateInstance(source.GetType())!;
            return Copy(source, target);
        }

        public object PushInheritance(object source, object target)
        {
            if (target.GetType().IsPrimitive || source.GetType().IsPrimitive) throw new InvalidOperationException();

            var commonType = TypeHelpers.FindCommonType(source.GetType(), target.GetType());
            if(commonType == null)
            {
                throw new InvalidOperationException("Could not find common type in PushInheritance!");
            }

            while (commonType != null)
            {
                if(TryGetDataDefinition(commonType, out var dataDef))
                    target = dataDef.InvokePushInheritanceDelegate(source, target, this);
                commonType = commonType.BaseType;
            }

            return target;
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
