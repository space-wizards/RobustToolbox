using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager.Attributes;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Manager
{
    public class SerializationManager : ISerializationManager
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        private Dictionary<Type, SerializationDataDefinition> _dataDefinitions = new();

        public void Initialize()
        {
            //generating all datadefinitions except exposedata
            foreach (var directType in _reflectionManager.FindTypesWithAttribute<YamlDefinition>())
            {
                _dataDefinitions.Add(directType, new SerializationDataDefinition(directType, _reflectionManager));
            }
        }

        private SerializationDataDefinition? GetDataDefinition(Type type)
        {
            if (_dataDefinitions.TryGetValue(type, out var dataDefinition)) return dataDefinition;

            if (!typeof(IExposeData).IsAssignableFrom(type)) return null;

            dataDefinition = new SerializationDataDefinition(type, _reflectionManager);
            _dataDefinitions.Add(type, dataDefinition);

            return dataDefinition;

        }

        public object Populate(Type type, YamlObjectSerializer serializer)
        {
            var currentType = type;
            var dataDef = GetDataDefinition(type);
            if (dataDef == null) return Activator.CreateInstance(type)!;

            var obj = Activator.CreateInstance(dataDef.Type)!;

            while (currentType != null)
            {
                dataDef = GetDataDefinition(currentType);
                dataDef?.PopulateDelegate(obj, serializer);

                currentType = currentType.BaseType;
            }

            return obj;
        }

        public YamlMappingNode? Serialize(Type type, object obj, YamlObjectSerializer.Context? context = null)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var currentType = type;
            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping, context);

            while (currentType != null)
            {
                var dataDef = GetDataDefinition(type);
                if (dataDef?.CanCallWith(obj) != true)
                    throw new ArgumentException($"Supplied parameter does not fit with datadefinition of {type}.", nameof(obj));
                dataDef?.SerializeDelegate(obj, serializer);
                currentType = currentType.BaseType;
            }

            return mapping;
        }

    }
}
