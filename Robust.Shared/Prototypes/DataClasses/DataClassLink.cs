using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Shared.Prototypes.DataClasses
{
    public class DataClassLink
    {
        public readonly Type Type;
        public readonly Type DataClassType;

        private List<LinkEntry> _actualFields = new();
        private List<LinkEntry> _dataclassFields = new();

        public readonly Action<object, DataClass> PopulateObjectDelegate;
        public readonly Action<object, DataClass> PopulateDataclassDelegate;

        public DataClassLink(Type type, Type dataClassType)
        {
            Type = type;
            DataClassType = dataClassType;

            foreach (var abstractFieldInfo in type.GetAllPropertiesAndFields())
            {
                var attr = abstractFieldInfo.GetCustomAttribute<BaseDataFieldAttribute>();
                if(attr == null) continue;

                _actualFields.Add(new LinkEntry(abstractFieldInfo, attr));
            }

            foreach (var abstractFieldInfo in dataClassType.GetAllPropertiesAndFields())
            {
                var attr = abstractFieldInfo.GetCustomAttribute<BaseDataFieldAttribute>();
                if(attr == null) continue;

                _dataclassFields.Add(new LinkEntry(abstractFieldInfo, attr));
            }

            PopulateObjectDelegate = EmitPopulateObjectDelegate();
            PopulateDataclassDelegate = EmitPopulateDataclassDelegate();
        }

        private Action<object, DataClass> EmitPopulateObjectDelegate()
        {
            var dynamicMethod = new DynamicMethod(
                $"_populateObjectFromDC<>{Type}<>{DataClassType}",
                typeof(void),
                new[] {typeof(object), typeof(DataClass), typeof(SerializationManager)},
                Type,
                true);
            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "obj");
            dynamicMethod.DefineParameter(2, ParameterAttributes.In, "dataclass");
            dynamicMethod.DefineParameter(3, ParameterAttributes.In, "serv3Mgr");
            var generator = dynamicMethod.GetILGenerator();

            foreach (var actualField in _actualFields)
            {
                LinkEntry? counterPart = null;
                foreach (var dataclassField in _dataclassFields)
                {
                    if (dataclassField.DataFieldAttribute.Tag == actualField.DataFieldAttribute.Tag)
                    {
                        counterPart = dataclassField;
                        break;
                    }
                }

                if (counterPart == null)
                    throw new InvalidOperationException(
                        "Could not find field-counterpart while generating PopulateObjectDelegate!");

                generator.EmitCopy(1, counterPart.FieldInfo, 0, actualField.FieldInfo, 2);
            }

            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<object, DataClass>>();
        }

        private Action<object, DataClass> EmitPopulateDataclassDelegate()
        {
            var dynamicMethod = new DynamicMethod(
                $"_populateDCFromObject<>{Type}<>{DataClassType}",
                typeof(void),
                new[] {typeof(object), typeof(DataClass), typeof(SerializationManager)},
                Type,
                true);
            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "obj");
            dynamicMethod.DefineParameter(2, ParameterAttributes.In, "dataclass");
            dynamicMethod.DefineParameter(3, ParameterAttributes.In, "serv3Mgr");
            var generator = dynamicMethod.GetILGenerator();

            foreach (var actualField in _actualFields)
            {
                LinkEntry? counterPart = null;
                foreach (var dataclassField in _dataclassFields)
                {
                    if (dataclassField.DataFieldAttribute.Tag == actualField.DataFieldAttribute.Tag)
                    {
                        counterPart = dataclassField;
                        break;
                    }
                }

                if (counterPart == null)
                    throw new InvalidOperationException(
                        "Could not find field-counterpart while generating PopulateDataclassDelegate!");

                generator.EmitCopy(0, actualField.FieldInfo, 1, counterPart.FieldInfo, 2);
            }

            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<object, DataClass>>();
        }

        private class LinkEntry
        {
            public readonly AbstractFieldInfo FieldInfo;
            public readonly BaseDataFieldAttribute DataFieldAttribute;

            public LinkEntry(AbstractFieldInfo fieldInfo, BaseDataFieldAttribute dataFieldAttribute)
            {
                FieldInfo = fieldInfo;
                DataFieldAttribute = dataFieldAttribute;
            }
        }
    }
}
