using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes
{
    public interface IComponentDataManager
    {
        Dictionary<string, object?> ParseComponentData(string compName, YamlMappingNode mapping);

        IYamlFieldDefinition[] GetComponentDataDefinition(string compName);

        Dictionary<string, object?> GetEmptyComponentData(string compName);

        void PopulateComponent(IComponent comp, Dictionary<string, object?> values);

        bool ValidateComponentData(string compName, Dictionary<string, object?> data);

        void PushInheritance(string compName, Dictionary<string, object?> source, Dictionary<string, object?> target);
    }

    public interface IYamlFieldDefinition
    {
        string Tag { get; }

        Type FieldType { get; }

        void SetValue(object obj, object? value);
    }

    public class YamlFieldDefinition : IYamlFieldDefinition
    {
        public string Tag { get; }
        public Type FieldType => _fieldInfo.FieldType;
        private FieldInfo _fieldInfo;

        public YamlFieldDefinition([NotNull] string tag, FieldInfo fieldInfo)
        {
            Tag = tag;
            _fieldInfo = fieldInfo;
        }

        public void SetValue(object obj, object? value)
        {
            _fieldInfo.SetValue(obj, value);
        }
    }

    public class YamlPropertyDefinition : IYamlFieldDefinition
    {
        public string Tag { get; }
        public Type FieldType => _propertyInfo.PropertyType;
        private PropertyInfo _propertyInfo;
        public YamlPropertyDefinition([NotNull] string tag, PropertyInfo propertyInfo)
        {
            Tag = tag;
            _propertyInfo = propertyInfo;
        }

        public void SetValue(object obj, object? value)
        {
            _propertyInfo.SetValue(obj, value);
        }
    }
}
