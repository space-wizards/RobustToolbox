using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager
{
    public class SerializationDataDefinition
    {
        public readonly Type Type;

        public IReadOnlyList<BaseFieldDefinition> FieldDefinitions => _baseFieldDefinitions;

        private readonly List<BaseFieldDefinition> _baseFieldDefinitions = new();

        public readonly Action<object, YamlObjectSerializer, SerializationManager> PopulateDelegate;

        public readonly Action<object, YamlObjectSerializer, SerializationManager, bool> SerializeDelegate;

        public readonly Action<object, object, SerializationManager> PushInheritanceDelegate;

        public bool CanCallWith(object obj) => Type.IsInstanceOfType(obj);

        public SerializationDataDefinition(Type type)
        {
            Type = type;
            var dummyObj = Activator.CreateInstance(type)!;

            foreach (var field in type.GetAllFields())
            {
                if(field.DeclaringType != type) continue;
                var attr = (YamlFieldAttribute?)Attribute.GetCustomAttribute(field, typeof(YamlFieldAttribute));
                if(attr == null) continue;
                _baseFieldDefinitions.Add(new FieldDefinition(attr, field.GetValue(dummyObj), field));
            }

            foreach (var property in type.GetAllProperties())
            {
                if(property.DeclaringType != type) continue;
                var attr = (YamlFieldAttribute?)Attribute.GetCustomAttribute(property, typeof(YamlFieldAttribute));
                if(attr == null) continue;
                _baseFieldDefinitions.Add(new PropertyDefinition(attr, property.GetValue(dummyObj), property));
            }

            PopulateDelegate = EmitPopulateDelegate();
            SerializeDelegate = EmitSerializeDelegate();
            PushInheritanceDelegate = EmitPushInheritanceDelegate();
        }

        private Action<object, YamlObjectSerializer, SerializationManager> EmitPopulateDelegate()
        {
            var dynamicMethod = new DynamicMethod(
                $"_populateDelegate<>{Type}",
                typeof(void),
                new[] {typeof(object), typeof(YamlObjectSerializer), typeof(SerializationManager)},
                Type,
                true);
            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "obj");
            dynamicMethod.DefineParameter(2, ParameterAttributes.In, "serializer");
            dynamicMethod.DefineParameter(3, ParameterAttributes.In, "serializationManager");
            var generator = dynamicMethod.GetILGenerator();

            foreach (var fieldDefinition in _baseFieldDefinitions)
            {
                generator.EmitPopulateField(fieldDefinition);
            }

            if (typeof(IExposeData).IsAssignableFrom(Type))
            {
                generator.EmitExposeDataCall();
            }

            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<object, YamlObjectSerializer, SerializationManager>>();
        }

        private Action<object, YamlObjectSerializer, SerializationManager, bool> EmitSerializeDelegate()
        {
            var dynamicMethod = new DynamicMethod(
                $"_serializeDelegate<>{Type}",
                typeof(void),
                new[] {typeof(object), typeof(YamlObjectSerializer), typeof(SerializationManager), typeof(bool)},
                Type,
                true);
            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "obj");
            dynamicMethod.DefineParameter(2, ParameterAttributes.In, "serializer");
            dynamicMethod.DefineParameter(3, ParameterAttributes.In, "serializationManager");
            dynamicMethod.DefineParameter(3, ParameterAttributes.In, "alwaysWrite");
            var generator = dynamicMethod.GetILGenerator();

            if (typeof(IExposeData).IsAssignableFrom(Type))
            {
                generator.EmitExposeDataCall();
            }

            foreach (var fieldDefinition in _baseFieldDefinitions)
            {
                generator.EmitSerializeField(fieldDefinition);
            }

            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<object, YamlObjectSerializer, SerializationManager, bool>>();
        }

        private Action<object, object, SerializationManager> EmitPushInheritanceDelegate()
        {
            var dynamicMethod = new DynamicMethod(
                $"_serializeDelegate<>{Type}",
                typeof(void),
                new[] {typeof(object), typeof(object), typeof(SerializationManager)},
                Type,
                true);
            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "source");
            dynamicMethod.DefineParameter(2, ParameterAttributes.In, "target");
            dynamicMethod.DefineParameter(3, ParameterAttributes.In, "serializationManager");
            var generator = dynamicMethod.GetILGenerator();

            if (typeof(IExposeData).IsAssignableFrom(Type))
            {
                var warnMethod = typeof(Logger).GetMethod(nameof(Logger.WarningS));
                generator.Emit(OpCodes.Ldstr,"SERV3");
                generator.Emit(OpCodes.Ldstr, $"PushInheritance is not supported for IExposeData (Type: {Type})");
                generator.Emit(OpCodes.Call, warnMethod!);
            }

            foreach (var fieldDefinition in _baseFieldDefinitions)
            {
                generator.EmitPushInheritanceField(fieldDefinition);
            }

            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<object, object, SerializationManager>>();
        }

        public abstract class BaseFieldDefinition
        {
            public readonly YamlFieldAttribute Attribute;
            public readonly object? DefaultValue;

            protected BaseFieldDefinition(YamlFieldAttribute attr, object? defaultValue)
            {
                Attribute = attr;
                DefaultValue = defaultValue;
            }

            public abstract Type FieldType { get; }
        }

        public class FieldDefinition : BaseFieldDefinition
        {
            public readonly FieldInfo FieldInfo;
            public override Type FieldType => FieldInfo.FieldType;


            public FieldDefinition(YamlFieldAttribute attr, object? defaultValue, FieldInfo fieldInfo) : base(attr, defaultValue)
            {
                FieldInfo = fieldInfo;
            }
        }

        public class PropertyDefinition : BaseFieldDefinition
        {
            public readonly PropertyInfo PropertyInfo;
            public override Type FieldType => PropertyInfo.PropertyType;

            public PropertyDefinition(YamlFieldAttribute attr, object? defaultValue, PropertyInfo propertyInfo) : base(attr, defaultValue)
            {
                PropertyInfo = propertyInfo;
            }
        }
    }
}
