using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

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
            InitializeDataClasses();

            foreach (var meansAttr in _reflectionManager.FindTypesWithAttribute<MeansDataDefinition>())
            {
                foreach (var type in _reflectionManager.FindTypesWithAttribute(meansAttr))
                {
                    _dataDefinitions.Add(type, new SerializationDataDefinition(type));
                }
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

        public T ReadValue<T>(IDataNode node, ISerializationContext? context = null)
        {
            return (T)ReadValue(typeof(T), node, context);
        }

        public object ReadValue(Type type, IDataNode node, ISerializationContext? context = null)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (node is IMappingDataNode mapping && mapping.TryGetNode("!type", out var typeNode) && typeNode is IValueDataNode typeValueDataNode)
            {
                underlyingType = ResolveConcreteType(underlyingType, typeValueDataNode.GetValue());
            }

            if (TryReadWithTypeSerializers(underlyingType, node, out var serializedObj, context))
            {
                return serializedObj;
            }

            if (typeof(ISelfSerialize).IsAssignableFrom(underlyingType))
            {
                if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();

                var selfSerObj = (ISelfSerialize) Activator.CreateInstance(underlyingType)!;
                selfSerObj.Deserialize(valueDataNode.GetValue());
                return selfSerObj;
            }

            if (node is not IMappingDataNode mappingDataNode) throw new InvalidNodeTypeException();

            var currentType = underlyingType;
            var dataDef = GetDataDefinition(underlyingType);
            if (dataDef == null) return Activator.CreateInstance(underlyingType)!;

            var obj = Activator.CreateInstance(dataDef.Type)!;

            if(obj is IPopulateDefaultValues populateDefaultValues)
                populateDefaultValues.PopulateDefaultValues();

            while (currentType != null)
            {
                if(TryGetDataDefinition(currentType, out dataDef))
                    obj = dataDef.InvokePopulateDelegate(obj, mappingDataNode, this, context);

                currentType = currentType.BaseType;
            }

            if (obj is ISerializationHooks serHooks)
                serHooks.AfterDeserialization();

            return obj;
        }

        public IDataNode WriteValue<T>(T value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null) where T : notnull
        {
            return WriteValue(typeof(T), value, nodeFactory, alwaysWrite, context);
        }

        public IDataNode WriteValue(Type type, object value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            //todo paul
            //var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (value == null) return nodeFactory.GetMappingNode();

            if (value is ISerializationHooks serHook)
                serHook.BeforeSerialization();

            if (TryWriteWithTypeSerializers(type, value, nodeFactory, out var node, alwaysWrite, context))
            {
                return node;
            }

            if (typeof(ISelfSerialize).IsAssignableFrom(type))
            {
                var selfSerObj = (ISelfSerialize)value;
                return nodeFactory.GetValueNode(selfSerObj.Serialize());
            }

            var currentType = type;
            var mapping = nodeFactory.GetMappingNode();
            if (type != value.GetType() && (type.IsAbstract || type.IsInterface))
            {
                mapping.AddNode("!type", nodeFactory.GetValueNode(value.GetType().Name));
                currentType = value.GetType();
            }

            while (currentType != null)
            {
                if (TryGetDataDefinition(currentType, out var dataDef))
                {
                    if (dataDef.CanCallWith(value) != true)
                        throw new ArgumentException($"Supplied parameter does not fit with datadefinition of {type}.", nameof(value));
                    var newMapping = dataDef.InvokeSerializeDelegate(value, this, nodeFactory, context, alwaysWrite);
                    mapping = mapping.Merge(newMapping);
                }
                currentType = currentType.BaseType;
            }

            return mapping;
        }

        public object Copy(object source, object target)
        {
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
                    target = dataDef.InvokePushInheritanceDelegate(source, target, this);
                commonType = commonType.BaseType;
            }

            return target;
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
                    target = dataDef.InvokeCopyDelegate(source, target, this);
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
