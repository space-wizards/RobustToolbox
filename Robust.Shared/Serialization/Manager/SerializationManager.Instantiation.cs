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

    /// <summary>
    /// This generates IL code that will try to invoke a record's constructor by passing default values to any arguments.
    /// </summary>
    private static void CreateRecordInstantiator(ILGenerator generator, Type type)
    {
        var constructors = type.GetConstructors();
        if (constructors.Length == 0)
        {
            if (!type.IsValueType)
                throw new ArgumentException($"Could not find a constructor for record class {type}");

            // Handle constructorless data record struct by treating it like a normal struct
            CreateValueTypeInstantiator(generator, type);
            return;
        }

        var constructor = constructors[0];
        foreach (var parameter in constructor.GetParameters())
        {
            var parameterType = parameter.ParameterType;

            if (parameterType.IsPrimitive)
            {

                if (parameterType == typeof(decimal))
                {
                    // I CBF figuring out how to support them, so fuck it.
                    throw new NotSupportedException($"Record class {type} contains decimals. DataRecords don't currently support decimals.");
                    // If anyone wants to try, a value of 0 looks like this in IL:
                    // > ldsfld valuetype [System.Runtime]System.Decimal [System.Runtime]System.Decimal::Zero
                    // While a default value of -1 uses another static field:
                    // > ldsfld valuetype [System.Runtime]System.Decimal [System.Runtime]System.Decimal::MinusOne
                }

                if (parameterType == typeof(float))
                {
                    var floatDefault = parameter.HasDefaultValue ? (float) parameter.DefaultValue! : 0f;
                    generator.Emit(OpCodes.Ldc_R4, floatDefault);
                }
                else if (parameterType == typeof(double))
                {
                    var doubleDefault = parameter.HasDefaultValue ? (double) parameter.DefaultValue! : 0d;
                    generator.Emit(OpCodes.Ldc_R8, doubleDefault);
                }
                else if (parameterType == typeof(nint) || parameterType == typeof(nuint))
                {
                    int nintDefault = parameter.HasDefaultValue ? (int)Convert.ToInt64(parameter.DefaultValue) : 0;
                    generator.Emit(OpCodes.Ldc_I4, nintDefault);

                    if (parameterType == typeof(nuint) && nintDefault < 0) // I'm only like 50% sure this is correct, but it makes the tests pass, so....
                        generator.Emit(OpCodes.Conv_U);
                    else
                        generator.Emit(OpCodes.Conv_I);
                }
                else if (parameterType == typeof(long) || parameterType == typeof(ulong))
                {
                    var longDefault = 0L;
                    if (parameter.HasDefaultValue)
                    {
                        longDefault = parameterType == typeof(ulong)
                            ? (long) (ulong) parameter.DefaultValue!
                            : Convert.ToInt64(parameter.DefaultValue);
                    }

                    generator.Emit(OpCodes.Ldc_I8, longDefault);
                }
                else
                {
                    var intDefault = 0;
                    if (parameter.HasDefaultValue)
                    {
                        intDefault = parameterType == typeof(uint)
                            ? (int) (uint) parameter.DefaultValue!
                            : Convert.ToInt32(parameter.DefaultValue);
                    }

                    generator.Emit(OpCodes.Ldc_I4, intDefault);
                }
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

            if (isRecord)
            {
                CreateRecordInstantiator(generator, type);
            }
            else if (type.IsValueType)
            {
                CreateValueTypeInstantiator(generator, type);
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
