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

        YamlMappingNode? SerializeNonDefaultComponentData(IComponent comp, YamlObjectSerializer.Context? context = null);

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

        bool IsCustom { get; }

        void SetValue(object obj, object? value);

        object? GetValue(object obj);
    }

    public class YamlFieldDefinition : IYamlFieldDefinition
    {
        public string Tag { get; }
        public Type FieldType => _fieldInfo.FieldType;
        public bool IsCustom { get; }
        private FieldInfo _fieldInfo;

        public YamlFieldDefinition([NotNull] string tag, FieldInfo fieldInfo, bool isCustom)
        {
            Tag = tag;
            _fieldInfo = fieldInfo;
            IsCustom = isCustom;
        }

        public void SetValue(object obj, object? value)
        {
            _fieldInfo.SetValue(obj, value);
        }

        public object? GetValue(object obj)
        {
            return _fieldInfo.GetValue(obj);
        }
    }

    public class YamlPropertyDefinition : IYamlFieldDefinition
    {
        public string Tag { get; }
        public Type FieldType => _propertyInfo.PropertyType;
        public bool IsCustom { get; }
        private PropertyInfo _propertyInfo;
        public YamlPropertyDefinition([NotNull] string tag, PropertyInfo propertyInfo, bool isCustom)
        {
            Tag = tag;
            _propertyInfo = propertyInfo;
            IsCustom = isCustom;
        }

        public void SetValue(object obj, object? value)
        {
            _propertyInfo.SetValue(obj, value);
        }

        public object? GetValue(object obj)
        {
            return _propertyInfo.GetValue(obj);
        }
    }
}
