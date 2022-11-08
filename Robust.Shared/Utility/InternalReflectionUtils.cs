using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Robust.Shared.Utility;

public static class InternalReflectionUtils
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
                var setter = property.PropertyInfo.GetSetMethod(true) ?? throw new NullReferenceException();

                var opCode = info.DeclaringType?.IsValueType ?? false
                    ? OpCodes.Call
                    : OpCodes.Callvirt;

                rGenerator.Emit(opCode, setter);
                break;
        }
    }

    internal static AccessField<T, object?> EmitFieldAccessor<T>(Type type, AbstractFieldInfo fieldDefinition)
    {
        var method = new DynamicMethod(
            "AccessField",
            typeof(object),
            new[] {typeof(T).MakeByRefType()},
            true);

        method.DefineParameter(1, ParameterAttributes.Out, "target");

        var generator = method.GetILGenerator();

        generator.Emit(OpCodes.Ldarg_0);

        switch (fieldDefinition)
        {
            case SpecificFieldInfo field:
                generator.Emit(OpCodes.Ldfld, field.FieldInfo);
                break;
            case SpecificPropertyInfo property:
                var getter = property.PropertyInfo.GetGetMethod(true) ?? throw new NullReferenceException();
                var opCode = type.IsValueType ? OpCodes.Call : OpCodes.Callvirt;
                generator.Emit(opCode, getter);
                break;
        }

        var returnType = fieldDefinition.FieldType;
        if (returnType.IsValueType)
        {
            generator.Emit(OpCodes.Box, returnType);
        }

        generator.Emit(OpCodes.Ret);

        return method.CreateDelegate<AccessField<T, object?>>();
    }

    internal static AssignField<T, object?> EmitFieldAssigner<T>(Type type, Type fieldType, AbstractFieldInfo backingField)
    {
        var method = new DynamicMethod(
            "AssignField",
            typeof(void),
            new[] {typeof(T).MakeByRefType(), typeof(object)},
            true);

        method.DefineParameter(1, ParameterAttributes.Out, "target");
        method.DefineParameter(2, ParameterAttributes.None, "value");

        var generator = method.GetILGenerator();

        if (type.IsValueType)
        {
            generator.DeclareLocal(type);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldind_Ref);
            generator.Emit(OpCodes.Unbox_Any, type);
            generator.Emit(OpCodes.Stloc_0);
            generator.Emit(OpCodes.Ldloca, 0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Unbox_Any, fieldType);

            EmitSetField(generator, backingField);

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Box, type);
            generator.Emit(OpCodes.Stind_Ref);

            generator.Emit(OpCodes.Ret);
        }
        else
        {
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldind_Ref);
            generator.Emit(OpCodes.Castclass, type);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Unbox_Any, fieldType);

            EmitSetField(generator, backingField.GetBackingField() ?? backingField);

            generator.Emit(OpCodes.Ret);
        }

        return method.CreateDelegate<AssignField<T, object?>>();
    }
}
