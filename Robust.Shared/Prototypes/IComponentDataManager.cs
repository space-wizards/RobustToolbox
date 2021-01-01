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
        ComponentData ParseComponentData(string compName, YamlMappingNode mapping, YamlObjectSerializer.Context? context = null);

        YamlMappingNode? SerializeNonDefaultComponentData(IComponent comp);

        IYamlFieldDefinition[] GetComponentDataDefinition(string compName);

        ComponentData GetEmptyComponentData(string compName);

        void PopulateComponent(IComponent comp, ComponentData values);

        void PushInheritance(string compName, ComponentData source, ComponentData target);

        void RegisterCustomDataClasses();
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
