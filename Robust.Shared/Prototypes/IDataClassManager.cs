using System;
using System.Reflection;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes
{
    public interface IDataClassManager
    {
        /// <summary>
        /// Returns an empty dataclass for type <paramref name="classType"/>
        /// </summary>
        /// <param name="classType"></param>
        /// <returns></returns>
        DataClass GetEmptyDataClass(Type classType);

        /// <summary>
        /// Returns an empty dataclass for <paramref name="compName"/>
        /// </summary>
        /// <param name="compName"></param>
        /// <returns></returns>
        DataClass GetEmptyComponentDataClass(string compName);

        /// <summary>
        /// Populates the <paramref name="obj"/> with data from the <paramref name="data"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="data"></param>
        void Populate(object obj, DataClass data);

        /// <summary>
        /// Fills all null-values of <paramref name="target"/> with the corresponding values of <paramref name="source"/>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <typeparam name="T">The dataclass type</typeparam>
        void PushInheritance<T>(T source, T target) where T : DataClass;

        /// <summary>
        /// Reads <paramref name="mapping"/> into a dataclass & returns it.
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="mapping"></param>
        /// <param name="context"></param>
        /// <returns>The new dataclass</returns>
        DataClass Parse(Type classType, YamlMappingNode mapping, YamlObjectSerializer.Context? context = null);

        /// <summary>
        /// Serializes all non-default values into a <see cref="YamlMappingNode"/>
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public YamlMappingNode? SerializeNonDefaultData(object obj,
            YamlObjectSerializer.Context? context = null);

        public YamlMappingNode? SerializeNonDefaultComponentData(IComponent comp,
            YamlObjectSerializer.Context? context = null);

    }

    public interface IYamlFieldDefinition
    {
        string Tag { get; }

        bool IsCustom { get; }

        int Priority { get; }

        void SetValue(object obj, object? value);

        object? GetValue(object obj);
    }

    public class YamlFieldDefinition : IYamlFieldDefinition
    {
        public string Tag { get; }
        public bool IsCustom { get; }
        public int Priority { get; }
        private FieldInfo _fieldInfo;

        public YamlFieldDefinition([NotNull] string tag, FieldInfo fieldInfo, bool isCustom, int? priority)
        {
            Tag = tag;
            _fieldInfo = fieldInfo;
            IsCustom = isCustom;
            Priority = priority ?? 1;
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
        public bool IsCustom { get; }
        public int Priority { get; }
        private PropertyInfo _propertyInfo;
        public YamlPropertyDefinition([NotNull] string tag, PropertyInfo propertyInfo, bool isCustom, int? priority)
        {
            Tag = tag;
            _propertyInfo = propertyInfo;
            IsCustom = isCustom;
            Priority = priority ?? 1;
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
