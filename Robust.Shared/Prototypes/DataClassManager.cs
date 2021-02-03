using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes
{
    public class DataClassManager : IDataClassManager
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        #region DataStructureDefinition

        private Dictionary<Type, IYamlFieldDefinition[]> _fieldDefinitions = new();

        private IYamlFieldDefinition[] GetDataStructure(Type type)
        {
            if (_fieldDefinitions.TryGetValue(type, out var struc))
            {
                return struc;
            }

            return CacheDataStructure(type);
        }

        private IYamlFieldDefinition[] CacheDataStructure(Type type)
        {
            var fields = new List<IYamlFieldDefinition>();

            foreach (var fieldInfo in type.GetAllFields())
            {
                BaseYamlField? yamlField =
                    (BaseYamlField?) Attribute.GetCustomAttribute(fieldInfo, typeof(YamlFieldAttribute)) ??
                    (BaseYamlField?) Attribute.GetCustomAttribute(fieldInfo, typeof(CustomYamlFieldAttribute));
                if(yamlField == null) continue;

                fields.Add(new YamlFieldDefinition(yamlField.Tag, fieldInfo, yamlField.GetType() == typeof(CustomYamlFieldAttribute), yamlField.Priority));
            }

            foreach (var propertyInfo in type.GetAllProperties())
            {
                BaseYamlField? yamlField =
                    (BaseYamlField?) Attribute.GetCustomAttribute(propertyInfo, typeof(YamlFieldAttribute)) ??
                    (BaseYamlField?) Attribute.GetCustomAttribute(propertyInfo, typeof(CustomYamlFieldAttribute));

                if(yamlField == null) continue;

                fields.Add(new YamlPropertyDefinition(yamlField.Tag, propertyInfo, yamlField.GetType() == typeof(CustomYamlFieldAttribute), yamlField.Priority));
            }

            var fieldArr = fields.ToArray();
            _fieldDefinitions[type] = fieldArr;
            return fieldArr;
        }

        #endregion

        #region Helpers

        private object?[] ReadFromObject(object obj)
        {
            var dataStructure = GetDataStructure(obj.GetType());
            var res = new object?[dataStructure.Length];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = dataStructure[i].GetValue(obj);
            }

            return res;
        }

        private void WriteToObject(object obj, object?[] data)
        {
            var dataStructure = GetDataStructure(obj.GetType());
            if (dataStructure.Length != data.Length)
                throw new InvalidOperationException("Length mismatch in WriteToObject!");
            for (int i = 0; i < data.Length; i++)
            {
                dataStructure[i].SetValue(obj, data[i]);
            }
        }

        private object?[] ReadFromDataClass(DataClass dataClass)
        {
            var dataStructure = GetDataStructure(dataClass.GetType());
            var res = new object?[dataStructure.Length];
            for (int i = 0; i < res.Length; i++)
            {
                res[i] = ReadFromDataClassField(dataClass, dataStructure[i]);
            }

            return res;
        }

        private object? ReadFromDataClassField(DataClass dataClass, IYamlFieldDefinition fieldDefinition)
        {
            if (!fieldDefinition.IsCustom)
            {
                //not custom field, we can avoid using reflection
                return dataClass.GetValue(fieldDefinition.Tag);
            }

            return fieldDefinition.GetValue(dataClass);
        }

        private void WriteToDataClass(DataClass dataClass, object?[] data)
        {
            var dataStructure = GetDataStructure(dataClass.GetType());
            if (dataStructure.Length != data.Length)
                throw new InvalidOperationException("Length mismatch in WriteToDataClass!");
            for (int i = 0; i < data.Length; i++)
            {
                WriteToDataClassField(dataClass, dataStructure[i], data[i]);
            }
        }

        private void WriteToDataClassField(DataClass dataClass, IYamlFieldDefinition fieldDefinition, object? data)
        {
            if (!fieldDefinition.IsCustom)
            {
                //not custom field, we can avoid using reflection
                dataClass.SetValue(fieldDefinition.Tag, data);
                return;
            }
            fieldDefinition.SetValue(dataClass, data);
        }

        #endregion

        #region DataClassTypeGetter

        private Dictionary<Type, Type> _dataClasses = new();

        private Type GetDataClassType(Type type)
        {
            if (_dataClasses.TryGetValue(type, out var dataClassType))
            {
                return dataClassType;
            }

            return CacheDataClassType(type);
        }

        private Type CacheDataClassType(Type type)
        {
            Type dataClassType;
            var attr = (CustomDataClassAttribute?)Attribute.GetCustomAttribute(type, typeof(CustomDataClassAttribute));
            if (attr != null)
            {
                dataClassType = attr.ClassName;
            }
            else
            {
                var nonGenericType = type;
                if (nonGenericType.IsGenericType)
                {
                    nonGenericType = nonGenericType.GetGenericTypeDefinition();
                }

                var classType = _reflectionManager.GetType($"{nonGenericType.Namespace}.{nonGenericType.Name}_AUTODATA");
                if (classType == null)
                {
                    if (nonGenericType.BaseType == null)
                        throw new InvalidProgramException($"No Dataclass found for type {nonGenericType}");
                    dataClassType = CacheDataClassType(nonGenericType.BaseType);
                }
                else
                {
                    dataClassType = classType;
                }
            }

            _dataClasses.Add(type, dataClassType);

            return dataClassType;
        }

        #endregion

        #region DefaultValueGetter

        private Dictionary<Type, object?[]> _defaultValues = new();

        private object?[] GetDefaultValues(Type type)
        {
            if (!_defaultValues.TryGetValue(type, out var defaultValuesArray))
            {
                return CacheDefaultValues(type);
            }

            return defaultValuesArray;
        }

        private object?[] CacheDefaultValues(Type type)
        {
            var dataStructure = GetDataStructure(type);
            var dummyObj = Activator.CreateInstance(type)!;

            var defaultValueDict = new object?[dataStructure.Length];
            for (var i = 0; i < dataStructure.Length; i++)
            {
                defaultValueDict[i] = dataStructure[i].GetValue(dummyObj);
            }

            _defaultValues.Add(type, defaultValueDict);
            return defaultValueDict;
        }

        #endregion

        public DataClass GetEmptyDataClass(Type classType)
        {
            var dataClassType = GetDataClassType(classType);
            return (DataClass) Activator.CreateInstance(dataClassType)!;
        }

        public DataClass GetEmptyComponentDataClass(string compName)
        {
            return GetEmptyDataClass(_componentFactory.GetRegistration(compName).Type);
        }

        public void Populate(object obj, DataClass data)
        {
            //todo Paul: convert dataclasses into proper objects
            WriteToObject(obj, ReadFromDataClass(data));
        }

        public void PushInheritance<T>(T source, T target) where T : DataClass
        {
            var dataStructure = GetDataStructure(typeof(T));
            foreach (var fieldDefinition in dataStructure)
            {
                var targetVal = ReadFromDataClassField(target, fieldDefinition);
                if (targetVal == null)
                {
                    WriteToDataClassField(target, fieldDefinition, ReadFromDataClassField(source, fieldDefinition));
                }
            }
        }

        public DataClass Parse(Type classType, YamlMappingNode mapping, YamlObjectSerializer.Context? context = null)
        {
            var dataClass = GetEmptyDataClass(classType);
            var serializer = YamlObjectSerializer.NewReader(mapping, context);
            dataClass.ExposeData(serializer);
            return dataClass;
        }

        public YamlMappingNode? SerializeNonDefaultData(object obj, YamlObjectSerializer.Context? context = null)
        {
            var values = ReadFromObject(obj);
            var defaultValues = GetDefaultValues(obj.GetType());

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == defaultValues[i]) values[i] = null;
            }

            var dataClass = GetEmptyDataClass(obj.GetType());
            //todo Paul: convert data to values
            WriteToDataClass(dataClass, values);

            var mapping = new YamlMappingNode();
            var serializer = YamlObjectSerializer.NewWriter(mapping, context);
            dataClass.ExposeData(serializer);

            return mapping.Children.Count != 0 ? mapping : null;
        }

        public YamlMappingNode? SerializeNonDefaultComponentData(IComponent comp, YamlObjectSerializer.Context? context = null)
        {
            var mapping = SerializeNonDefaultData(comp, context);
            mapping?.Add("type", comp.Name);
            return mapping;
        }
    }
}
