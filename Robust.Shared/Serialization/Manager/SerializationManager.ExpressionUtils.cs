using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Robust.Shared.Serialization.Manager;

public sealed partial class SerializationManager
{
    public Expression NewExpressionDefault(Type type)
    {
        if (type.IsValueType)
            return Expression.Default(type);

        if (TryGetDefinition(type, out var def) && def.IsRecord)
        {
            var constructor = type.GetConstructors()[0];
            return Expression.New(constructor,
                constructor.GetParameters().Select(DefaultValueOrTypeDefault));
        }

        return Expression.New(type);
    }

    public static NewExpression NewExpression<T>(params object[] parameters) => NewExpression(typeof(T), parameters);

    public static NewExpression NewExpression(Type type, params object[] parameters)
    {
        return Expression.New(
            type.GetConstructor(parameters.Select(ExpressionTypeOrType).ToArray())!,
            parameters.Select(ExpressionOrConstant));
    }

    public static UnaryExpression ThrowExpression<T>(params object[] args) where T : Exception
    {
        return Expression.Throw(
            Expression.New(
                typeof(T).GetConstructor(args.Select(ExpressionTypeOrType).ToArray())!,
                args.Select(ExpressionOrConstant)));
    }

    public static Type ExpressionTypeOrType(object x) => x is Expression expr ? expr.Type : x.GetType();
    public static Expression ExpressionOrConstant(object x) => x is Expression expr ? expr : Expression.Constant(x);

    public static Expression DefaultValueOrTypeDefault(ParameterInfo x) => x.HasDefaultValue
        ? Expression.Constant(x.DefaultValue, x.ParameterType)
        : Expression.Default(x.ParameterType);

    public static Expression EqualExpression(Expression left, Expression right)
    {
        if (left.Type != right.Type)
            throw new InvalidOperationException(
                $"Left & Right Expression Types dont match ({left.Type}, {right.Type})");

        if (left.Type.IsPrimitive || left.Type == typeof(string) || left.Type.GetMethod("op_Equality", BindingFlags.Instance) != null)
        {
            return Expression.Equal(left, right);
        }

        var comparerType = typeof(EqualityComparer<>).MakeGenericType(left.Type);
        return Expression.Call(
            Expression.Constant(comparerType.GetProperty("Default")!.GetMethod!.Invoke(null, null)!, comparerType),
            "Equals",
            Type.EmptyTypes,
            left,
            right);
    }
}
