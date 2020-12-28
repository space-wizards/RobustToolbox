using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes
{
    public class ComponentDataManager : IComponentDataManager
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;

        #region Populating

        public void PopulateComponent(IComponent comp, Dictionary<string, object?> values)
        {
            if (!ValidateComponentData(comp.Name, values))
                throw new ArgumentException($"invalid Componentdata when populating {comp}", nameof(values));

            var def = GetComponentDataDefinition(comp.Name);

            foreach (var fieldDefinition in def)
            {
                var value = values[fieldDefinition.Tag];
                if(value == null) continue;
                fieldDefinition.SetValue(comp, value);
            }
        }

        public bool ValidateComponentData(string compName, Dictionary<string, object?> data)
        {
            var def = GetComponentDataDefinition(compName);

            var dataCopy = data.ShallowClone();

            foreach (var key in dataCopy.Keys)
            {
                if (def.All(d => d.Tag != key)) return false;
                dataCopy.Remove(key);
            }

            if (dataCopy.Count != 0) return false;

            return true;
        }

        public void PushInheritance(string compName, Dictionary<string, object?> source, Dictionary<string, object?> target)
        {
            if (!ValidateComponentData(compName, source))
                throw new ArgumentException("Invalid ComponentData", nameof(source));

            if (!ValidateComponentData(compName, target))
                throw new ArgumentException("Invalid ComponentData", nameof(target));

            foreach (var (tag, _) in source)
            {
                target[tag] ??= source[tag];
            }
        }

        #endregion

        #region Parsing

        private readonly Dictionary<string, IYamlFieldDefinition[]> _dataDefinitions = new();

        public YamlMappingNode? SerializeNonDefaultComponentData(IComponent comp)
        {
            var mapping = new YamlMappingNode
            {
                {"type", comp.Name}
            };
            //todo Paul: serialize all non-default (default & prototype) values#
            //todo Paul: return null if no non-default values
            throw new NotImplementedException();
        }

        public IYamlFieldDefinition[] GetComponentDataDefinition(string compName)
        {
            if (!_dataDefinitions.TryGetValue(compName, out var dataDefinition))
            {
                dataDefinition = GenerateAndCacheDataDefinition(compName);
            }

            return dataDefinition;
        }

        public Dictionary<string, object?> GetEmptyComponentData(string compName)
        {
            var def = GetComponentDataDefinition(compName);
            var dataDef = new Dictionary<string, object?>();
            foreach (var fieldDefinition in def)
            {
                dataDef.Add(fieldDefinition.Tag, null);
            }

            return dataDef;
        }

        private IYamlFieldDefinition[] GenerateAndCacheDataDefinition(string compName)
        {
            var compType = _componentFactory.GetRegistration(compName).Type;
            var dataDef = new List<IYamlFieldDefinition>();
            foreach (var fieldInfo in compType.GetAllFields())
            {
                var yamlFieldAttribute =
                    fieldInfo.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(YamlFieldAttribute));
                if (yamlFieldAttribute == null) continue;

                var tag = (string)yamlFieldAttribute.ConstructorArguments[0].Value!;
                dataDef.Add(new YamlFieldDefinition(tag, fieldInfo));
            }

            foreach (var propertyInfo in compType.GetAllProperties())
            {
                var yamlFieldAttribute =
                    propertyInfo.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(YamlFieldAttribute));
                if (yamlFieldAttribute == null) continue;

                var tag = (string)yamlFieldAttribute.ConstructorArguments[0].Value!;
                dataDef.Add(new YamlPropertyDefinition(tag, propertyInfo));
            }

            var res = dataDef.ToArray();

            _dataDefinitions.Add(compName, res);

            return res;
        }

        public Dictionary<string, object?> ParseComponentData(string compName, YamlMappingNode mapping)
        {
            var dataDefinition = GetComponentDataDefinition(compName);
            var ser = YamlObjectSerializer.NewReader(mapping);

            var data = new Dictionary<string, object?>();
            foreach (var fieldDef in dataDefinition)
            {
                object? value = null;

                if(mapping.TryGetNode(fieldDef.Tag, out var node))
                {
                    value = ser.NodeToType(fieldDef.FieldType, node);
                    mapping.Children.Remove(fieldDef.Tag);
                }

                data.Add(fieldDef.Tag, value);
            }

            if (mapping.Children.Count != 0)
                throw new PrototypeLoadException($"Not all values of component {compName} were consumed (Not consumed: {string.Join(',',mapping.Children.Select(n => n.Key))})");

            return data;
        }

        #endregion
    }
}
