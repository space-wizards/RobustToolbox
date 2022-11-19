using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace Robust.Shared.Serialization.Manager;

public partial class SerializationManager
{
    private readonly ConcurrentDictionary<Type, object> _instantiators = new();

    private static void CreateValueTypeInstantiator(ILGenerator generator, Type type)
    {
        var constructor = type.GetConstructor(Type.EmptyTypes);

        if (constructor == null)
        {
            generator.DeclareLocal(type);
            generator.Emit(OpCodes.Ldloca_S, 0);
            generator.Emit(OpCodes.Initobj, type);
            generator.Emit(OpCodes.Ldloc_0);
        }
        else
        {
            generator.Emit(OpCodes.Newobj, constructor);
        }

        generator.Emit(OpCodes.Ret);
    }

    private static void CreateClassInstantiator(ILGenerator generator, Type type)
    {
        if (type.IsArray)
        {
            throw new ArgumentException($"Tried instantiating unsupported type {type}.");
        }

        var constructor = type.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            Type.EmptyTypes);

        if (constructor == null)
            throw new ArgumentException($"Could not find an empty constructor for non-record class {type}");

        generator.Emit(OpCodes.Newobj, constructor);
        generator.Emit(OpCodes.Ret);
    }

    private static void CreateRecordInstantiator(ILGenerator generator, Type type)
    {
        var constructors = type.GetConstructors();
        if (constructors.Length == 0)
            throw new ArgumentException($"Could not find a constructor for record class {type}");

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
                var local = generator.DeclareLocal(parameterType);
                generator.Emit(OpCodes.Ldloca_S, local);
                generator.Emit(OpCodes.Initobj, parameterType);
                generator.Emit(OpCodes.Ldloc_0, local);
            }
            else
            {
                generator.Emit(OpCodes.Ldnull);
            }
        }

        generator.Emit(OpCodes.Newobj, constructor);
        generator.Emit(OpCodes.Ret);
    }

    internal ISerializationManager.InstantiationDelegate<T> GetOrCreateInstantiator<T>(bool isDataRecord, Type? actualType = null)
    {
        if (actualType != null && !actualType.IsAssignableTo(typeof(T)))
        {
            throw new ArgumentException(
                $"{nameof(actualType)} has to be a derived type of {typeof(T)} but was {actualType}!",
                nameof(actualType));
        }

        var type = actualType ?? typeof(T);

        return (ISerializationManager.InstantiationDelegate<T>)_instantiators.GetOrAdd(type, static (type, isRecord) =>
        {
            var method = new DynamicMethod(
                "Instantiator",
                type,
                Type.EmptyTypes,
                true);

            var generator = method.GetILGenerator();

            if (type.IsValueType)
            {
                CreateValueTypeInstantiator(generator, type);
            }
            else if (isRecord)
            {
                CreateRecordInstantiator(generator, type);
            }
            else
            {
                CreateClassInstantiator(generator, type);
            }

            return method.CreateDelegate(typeof(ISerializationManager.InstantiationDelegate<>).MakeGenericType(type));
        }, isDataRecord);
    }

    //we can safely set isDataRecord to false here due to a delegate already existing if it if it were
    private T InstantiateValue<T>() => GetOrCreateInstantiator<T>(false)();

    internal MethodCallExpression InstantiationExpression(ConstantExpression managerConst, Type type)
    {
        return Expression.Call(
            managerConst,
            nameof(InstantiateValue),
            new[] { type });
    }
}
