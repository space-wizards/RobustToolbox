using System;
using System.Collections.Concurrent;
using System.Reflection.Emit;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager;

public partial class SerializationManager
{
    internal delegate T InstantiationDelegate<out T>();

    private readonly ConcurrentDictionary<Type, InstantiationDelegate<object>> _instantiators = new();

    private static void CreateValueTypeInstantiator(ILGenerator generator, Type type)
    {
        generator.DeclareLocal(type);
        generator.Emit(OpCodes.Ldloca_S, 0);
        generator.Emit(OpCodes.Initobj, type);
        generator.Emit(OpCodes.Ldloc_0);
        generator.Emit(OpCodes.Box, type);
        generator.Emit(OpCodes.Ret);
    }

    private static void CreateClassInstantiator(ILGenerator generator, Type type)
    {
        var constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
            throw new ArgumentException($"Could not find an empty constructor for class {type}");

        generator.Emit(OpCodes.Newobj, constructor);
        generator.Emit(OpCodes.Ret);
    }

    private static void CreateRecordInstantiator(ILGenerator generator, Type type)
    {
        var constructors = type.GetConstructors();
        if (constructors.Length == 0)
            throw new ArgumentException($"Could not find a constructor for record {type}");

        var constructor = constructors[0];
        foreach (var parameter in constructor.GetParameters())
        {
            var parameterType = parameter.ParameterType;

            if (parameterType.IsPrimitive)
            {
                var defaultValue = Convert.ToInt64(parameter.HasDefaultValue ? parameter.DefaultValue! : 0);
                generator.Emit(OpCodes.Ldc_I4, defaultValue);

                if (parameterType == typeof(long) || parameterType == typeof(ulong))
                    generator.Emit(OpCodes.Conv_I8);
            }
            else if (parameterType.IsValueType)
            {
                generator.Emit(OpCodes.Initobj);
            }
            else
            {
                generator.Emit(OpCodes.Ldnull);
            }
        }

        generator.Emit(OpCodes.Newobj, constructor);
        generator.Emit(OpCodes.Ret);
    }

    private InstantiationDelegate<object> GetOrCreateInstantiator(Type type)
    {
        return _instantiators.GetOrAdd(type, static type =>
        {
            var method = new DynamicMethod(
                "Instantiator",
                typeof(object),
                new[] { typeof(object) },
                true);

            var generator = method.GetILGenerator();

            if (type.IsValueType)
            {
                CreateValueTypeInstantiator(generator, type);
            }
            else if (type.HasCustomAttribute<DataRecord>())
            {
                CreateRecordInstantiator(generator, type);
            }
            else
            {
                CreateClassInstantiator(generator, type);
            }

            return method.CreateDelegate<InstantiationDelegate<object>>();
        });
    }
}
