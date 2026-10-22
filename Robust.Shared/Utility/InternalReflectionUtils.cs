using System;
using System.Reflection;
using System.Reflection.Emit;
using Robust.Shared.Serialization.Manager.Definition;

namespace Robust.Shared.Utility;

internal static class InternalReflectionUtils
{
    internal delegate TValue AccessField<TTarget, TValue>(ref TTarget target);

    internal delegate void AssignField<TTarget, TValue>(ref TTarget target, TValue? value);


    private static void EmitSetField(ILGenerator rGenerator, AbstractFieldInfo info)
    {
        switch (info)
        {
            case SpecificFieldInfo field:
                rGenerator.Emit(OpCodes.Stfld, field.FieldInfo);
                break;
            case SpecificPropertyInfo property:
                if (property.GetBackingField() is { } backingField)
                {
                    rGenerator.Emit(OpCodes.Stfld, backingField.FieldInfo);
                    break;
                }

                var setter = property.PropertyInfo.GetSetMethod(true) ?? throw new NullReferenceException();

                var opCode = info.DeclaringType?.IsValueType ?? false
                    ? OpCodes.Call
                    : OpCodes.Callvirt;

                rGenerator.Emit(opCode, setter);
                break;
        }
    }

    internal static object EmitFieldAssigner(Type objType, AbstractFieldInfo backingField, bool boxing = false)
    {
        if (!boxing)
        {
            if (backingField is SpecificFieldInfo { FieldInfo.IsInitOnly: false } fieldInfo)
                return fieldInfo.FieldInfo;

            if (backingField is SpecificPropertyInfo propertyInfo)
            {
                if (propertyInfo.TryGetBackingField(out var propertyBackingField) && !propertyBackingField.FieldInfo.IsInitOnly)
                    return propertyBackingField.FieldInfo;

                if (propertyInfo.PropertyInfo.GetSetMethod(true) is { } setMethod)
                    return setMethod;
            }
        }

        var fieldType = backingField.FieldType;

        var method = new DynamicMethod(
            "AssignField",
            typeof(void),
            new[] {objType.MakeByRefType(), boxing ? typeof(object) : fieldType},
            true);

        method.DefineParameter(1, ParameterAttributes.Out, "target");
        method.DefineParameter(2, ParameterAttributes.None, "value");

        var generator = method.GetILGenerator();

        generator.Emit(OpCodes.Ldarg_0);

        if(!objType.IsValueType)
            generator.Emit(OpCodes.Ldind_Ref);

        generator.Emit(OpCodes.Ldarg_1);

        if (boxing)
        {
            generator.Emit(fieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, fieldType);
        }

        EmitSetField(generator, backingField);

        generator.Emit(OpCodes.Ret);

        return method.CreateDelegate(typeof(AssignField<,>).MakeGenericType(objType, boxing ? typeof(object) : fieldType));
    }
}
