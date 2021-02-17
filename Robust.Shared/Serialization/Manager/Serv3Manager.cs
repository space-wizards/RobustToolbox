using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public partial class Serv3Manager : IServ3Manager
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        private Dictionary<Type, SerializationDataDefinition> _dataDefinitions = new();

        public void Initialize()
        {
            InitializeTypeSerializers();
            InitializeDataClasses();

            //generating all datadefinitions except pure exposedata inheritors
            foreach (var meansAttr in _reflectionManager.FindTypesWithAttribute<MeansYamlDefinition>())
            {
                foreach (var type in _reflectionManager.FindTypesWithAttribute(meansAttr))
                {
                    _dataDefinitions.Add(type, new SerializationDataDefinition(type));
                }
            }
        }

        private SerializationDataDefinition? GetDataDefinition(Type type)
        {
            if (_dataDefinitions.TryGetValue(type, out var dataDefinition)) return dataDefinition;

            dataDefinition = new SerializationDataDefinition(type);
            _dataDefinitions.Add(type, dataDefinition);

            return dataDefinition;
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
            if (TryReadWithTypeSerializers(type, node, out var serializedObj, context))
            {
                return serializedObj;
            }

            if (node is not IMappingDataNode mappingDataNode) throw new InvalidNodeTypeException();

            var currentType = type;
            var dataDef = GetDataDefinition(type);
            if (dataDef == null) return Activator.CreateInstance(type)!;

            var obj = Activator.CreateInstance(dataDef.Type)!;

            while (currentType != null)
            {
                if(TryGetDataDefinition(currentType, out dataDef))
                    obj = dataDef.InvokePopulateDelegate(obj, mappingDataNode, this, context);

                currentType = currentType.BaseType;
            }

            return obj;
        }

        public IDataNode WriteValue<T>(T value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            throw new System.NotImplementedException();
        }

        public IDataNode WriteValue(Type type, object value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            if (TryWriteWithTypeSerializers(type, value, nodeFactory, out var node, alwaysWrite, context))
            {
                return node;
            }

            var currentType = type;
            var mapping = nodeFactory.GetMappingNode();

            while (currentType != null)
            {
                if (TryGetDataDefinition(type, out var dataDef))
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
            if (target.GetType().IsPrimitive || source.GetType().IsPrimitive)
            {
                if (!target.GetType().IsPrimitive || !source.GetType().IsPrimitive)
                    throw new InvalidOperationException();

                //todo paul copy primitive
            }

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
    }
}
