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
        private delegate object PopulateDelegateSignature(object target, ObjectSerializer serializer,
            ISerializationManager serializationManager, object?[] defaultValues);
        private delegate void SerializeDelegateSignature(object obj, ObjectSerializer serializer,
            ISerializationManager serializationManager, object?[] defaultValues, bool alwaysWrite);
        private delegate object PushInheritanceDelegateSignature(object source, object target,
            ISerializationManager serializationManager, object?[] defaultValues);
        public delegate object CopyDelegateSignature(object source, object target,
            ISerializationManager serializationManager);


        public readonly Type Type;

        public IReadOnlyList<FieldDefinition> FieldDefinitions => _baseFieldDefinitions;

        private readonly FieldDefinition[] _baseFieldDefinitions;
        private readonly object?[] _defaultValues;

        private readonly PopulateDelegateSignature _populateDelegate;

        private readonly SerializeDelegateSignature _serializeDelegate;

        private readonly PushInheritanceDelegateSignature _pushInheritanceDelegate;

        public readonly CopyDelegateSignature CopyDelegate;

        public bool CanCallWith(object obj) => Type.IsInstanceOfType(obj);

        private bool GetWarningSMethod(MethodInfo m)
        {
            return m.Name == nameof(Logger.WarningS) && m.GetParameters().Length == 2;
        }

        public SerializationDataDefinition(Type type)
        {
            Type = type;
            var dummyObj = Activator.CreateInstance(type)!;

            var fieldDefs = new List<FieldDefinition>();
            foreach (var abstractFieldInfo in type.GetAllPropertiesAndFields())
            {
                if(abstractFieldInfo.DeclaringType != type) continue;
                var attr = abstractFieldInfo.GetCustomAttribute<YamlFieldAttribute>();
                if(attr == null) continue;
                if (abstractFieldInfo is SpecificPropertyInfo propertyInfo)
                {
                    if (propertyInfo.PropertyInfo.GetMethod == null)
                    {
                        Logger.ErrorS("SerV3", $"Property {propertyInfo} is annotated with YamlFieldAttribute but has no getter");
                        continue;
                    }else if (!attr.ReadOnly && propertyInfo.PropertyInfo.SetMethod == null)
                    {
                        Logger.ErrorS("SerV3", $"Property {propertyInfo} is annotated with YamlFieldAttribute as non-readonly but has no setter");
                        continue;
                    }
                }
                fieldDefs.Add(new FieldDefinition(attr, abstractFieldInfo.GetValue(dummyObj), abstractFieldInfo));
            }

            _baseFieldDefinitions = fieldDefs.ToArray();
            _defaultValues = fieldDefs.Select(f => f.DefaultValue).ToArray();

            _populateDelegate = EmitPopulateDelegate();
            _serializeDelegate = EmitSerializeDelegate();
            _pushInheritanceDelegate = EmitPushInheritanceDelegate();
            CopyDelegate = EmitCopyDelegate();
        }

        public object InvokePopulateDelegate(object obj, ObjectSerializer serializer, SerializationManager serv3Mgr)
        {
            return _populateDelegate(obj, serializer, serv3Mgr, _defaultValues);
        }

        public void InvokeSerializeDelegate(object obj, ObjectSerializer ser, SerializationManager serv3Mgr,
            bool alwaysWrite)
        {
            _serializeDelegate(obj, ser, serv3Mgr, _defaultValues, alwaysWrite);
        }

        public object InvokePushInheritanceDelegate(object obj1, object obj2, SerializationManager serv3Mgr)
        {
            return _pushInheritanceDelegate(obj1, obj2, serv3Mgr, _defaultValues);
        }

        private PopulateDelegateSignature EmitPopulateDelegate()
        {
            var dynamicMethod = new DynamicMethod(
                $"_populateDelegate<>{Type}",
                typeof(object),
                new[] {typeof(object), typeof(ObjectSerializer), typeof(ISerializationManager), typeof(object?[])},
                Type,
                true);
            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "obj");
            dynamicMethod.DefineParameter(2, ParameterAttributes.In, "serializer");
            dynamicMethod.DefineParameter(3, ParameterAttributes.In, "serializationManager");
            dynamicMethod.DefineParameter(4, ParameterAttributes.In, "defaultValues");
            var generator = dynamicMethod.GetILGenerator();

            for (var i = 0; i < _baseFieldDefinitions.Length; i++)
            {
                var fieldDefinition = _baseFieldDefinitions[i];
                var idc = generator.DeclareLocal(fieldDefinition.FieldType).LocalIndex;
                generator.EmitPopulateField(fieldDefinition, idc, i);
            }

            if (typeof(IExposeData).IsAssignableFrom(Type))
            {
                generator.EmitExposeDataCall();
            }

            generator.Emit(OpCodes.Ldarg_0);
            //generator.Emit(OpCodes.Box, Type);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<PopulateDelegateSignature>();
        }

        private SerializeDelegateSignature EmitSerializeDelegate()
        {
            var dynamicMethod = new DynamicMethod(
                $"_serializeDelegate<>{Type}",
                typeof(void),
                new[] {typeof(object), typeof(ObjectSerializer), typeof(ISerializationManager), typeof(object?[]), typeof(bool)},
                Type,
                true);
            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "obj");
            dynamicMethod.DefineParameter(2, ParameterAttributes.In, "serializer");
            dynamicMethod.DefineParameter(3, ParameterAttributes.In, "serializationManager");
            dynamicMethod.DefineParameter(4, ParameterAttributes.In, "defaultValues");
            dynamicMethod.DefineParameter(5, ParameterAttributes.In, "alwaysWrite");
            var generator = dynamicMethod.GetILGenerator();

            if (typeof(IExposeData).IsAssignableFrom(Type))
            {
                generator.EmitExposeDataCall();
            }

            for (var i = 0; i < _baseFieldDefinitions.Length; i++)
            {
                var fieldDefinition = _baseFieldDefinitions[i];
                generator.EmitSerializeField(fieldDefinition, i);
            }

            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<SerializeDelegateSignature>();
        }

        private PushInheritanceDelegateSignature EmitPushInheritanceDelegate()
        {
            var dynamicMethod = new DynamicMethod(
                $"_serializeDelegate<>{Type}",
                typeof(object),
                new[] {typeof(object), typeof(object), typeof(ISerializationManager), typeof(object?[])},
                Type,
                true);
            dynamicMethod.DefineParameter(1, ParameterAttributes.In, "source");
            dynamicMethod.DefineParameter(2, ParameterAttributes.In, "target");
            dynamicMethod.DefineParameter(3, ParameterAttributes.In, "serializationManager");
            dynamicMethod.DefineParameter(4, ParameterAttributes.In, "defaultValues");
            var generator = dynamicMethod.GetILGenerator();

            if (typeof(IExposeData).IsAssignableFrom(Type))
            {
                generator.Emit(OpCodes.Ldstr, "SERV3");
                generator.Emit(OpCodes.Ldstr, $"PushInheritance is not supported for IExposeData (Type: {Type})");
                var warnMethod = typeof(Logger).GetMethods().First(GetWarningSMethod);
                Debug.Assert(warnMethod != null, nameof(warnMethod) + " != null");
                generator.Emit(OpCodes.Callvirt, warnMethod);
            }

            for (var i = 0; i < _baseFieldDefinitions.Length; i++)
            {
                var fieldDefinition = _baseFieldDefinitions[i];
                generator.EmitPushInheritanceField(fieldDefinition, i);
            }

            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Box, Type);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<PushInheritanceDelegateSignature>();
        }

        private CopyDelegateSignature EmitCopyDelegate()
        {
            var dynamicMethod = new DynamicMethod(
                $"_populateDelegate<>{Type}",
                typeof(object),
                new[] {typeof(object), typeof(object), typeof(ISerializationManager)},
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

            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Box, Type);
            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<CopyDelegateSignature>();
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
