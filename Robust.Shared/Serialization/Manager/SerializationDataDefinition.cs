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

        public IReadOnlyList<FieldDefinition> FieldDefinitions => _baseFieldDefinitions;

        private readonly List<FieldDefinition> _baseFieldDefinitions = new();

        public readonly Action<object, YamlObjectSerializer, SerializationManager> PopulateDelegate;

        public readonly Action<object, YamlObjectSerializer, SerializationManager, bool> SerializeDelegate;

        public readonly Action<object, object, SerializationManager> PushInheritanceDelegate;

        public readonly Action<object, object, SerializationManager> CopyDelegate;

        public bool CanCallWith(object obj) => Type.IsInstanceOfType(obj);

        private bool GetWarningSMethod(MethodInfo m)
        {
            return m.Name == nameof(Logger.WarningS) && m.GetParameters().Length == 2;
        }

        public SerializationDataDefinition(Type type)
        {
            Type = type;
            var dummyObj = Activator.CreateInstance(type)!;

            foreach (var field in type.GetAllFields())
            {
                if(field.DeclaringType != type) continue;
                var attr = (YamlFieldAttribute?)Attribute.GetCustomAttribute(field, typeof(YamlFieldAttribute));
                if(attr == null) continue;
                _baseFieldDefinitions.Add(new FieldDefinition(attr, field.GetValue(dummyObj), new SpecificFieldInfo(field)));
            }

            foreach (var property in type.GetAllProperties())
            {
                if(property.DeclaringType != type) continue;
                var attr = (YamlFieldAttribute?)Attribute.GetCustomAttribute(property, typeof(YamlFieldAttribute));
                if(attr == null) continue;
                _baseFieldDefinitions.Add(new FieldDefinition(attr, property.GetValue(dummyObj), new SpecificPropertyInfo(property)));
            }

            PopulateDelegate = EmitPopulateDelegate();
            SerializeDelegate = EmitSerializeDelegate();
            PushInheritanceDelegate = EmitPushInheritanceDelegate();
            CopyDelegate = EmitCopyDelegate();
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
                generator.Emit(OpCodes.Ldstr, "SERV3");
                generator.Emit(OpCodes.Ldstr, $"PushInheritance is not supported for IExposeData (Type: {Type})");
                var warnMethod = typeof(Logger).GetMethods().First(GetWarningSMethod);
                Debug.Assert(warnMethod != null, nameof(warnMethod) + " != null");
                generator.Emit(OpCodes.Call, warnMethod);
            }

            foreach (var fieldDefinition in _baseFieldDefinitions)
            {
                generator.EmitPushInheritanceField(fieldDefinition);
            }

            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<object, object, SerializationManager>>();
        }

        private Action<object, object, SerializationManager> EmitCopyDelegate()
        {
            var dynamicMethod = new DynamicMethod(
                $"_populateDelegate<>{Type}",
                typeof(void),
                new[] {typeof(object), typeof(object), typeof(SerializationManager)},
                Type,
                true);
            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "source");
            dynamicMethod.DefineParameter(2, ParameterAttributes.In, "target");
            dynamicMethod.DefineParameter(3, ParameterAttributes.In, "serv3Mgr");
            var generator = dynamicMethod.GetILGenerator();

            if (typeof(IExposeData).IsAssignableFrom(Type))
            {
                generator.Emit(OpCodes.Ldstr, "SERV3");
                generator.Emit(OpCodes.Ldstr, $"Copy is not supported for IExposeData (Type: {Type})");
                var warnMethod = typeof(Logger).GetMethods().First(GetWarningSMethod);
                Debug.Assert(warnMethod != null, nameof(warnMethod) + " != null");
                generator.Emit(OpCodes.Call, warnMethod);
            }

            foreach (var fieldDefinition in _baseFieldDefinitions)
            {
                generator.EmitCopy(0, fieldDefinition.FieldInfo, 1, fieldDefinition.FieldInfo, 2);
            }

            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<object, object, SerializationManager>>();
        }

        public class FieldDefinition
        {
            public readonly YamlFieldAttribute Attribute;
            public readonly object? DefaultValue;
            public readonly AbstractFieldInfo FieldInfo;

            public FieldDefinition(YamlFieldAttribute attr, object? defaultValue, AbstractFieldInfo fieldInfo)
            {
                Attribute = attr;
                DefaultValue = defaultValue;
                FieldInfo = fieldInfo;
            }

            public Type FieldType => FieldInfo.FieldType;
        }
    }
}
