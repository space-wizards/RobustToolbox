using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Robust.Shared.Utility;

public static class ExpressionUtils
{
    public static MethodCallExpression ToStringExpression(Expression expression)
    {
        return Expression.Call(
            Expression.Convert(expression, typeof(object)),
            "ToString",
            Type.EmptyTypes);
    }

    public static Expression WriteLine(object value)
    {
        if (value is Expression valExpr)
        {
            if (valExpr.Type != typeof(string))
            {
                value = ToStringExpression(valExpr);
            }
        }
        else if (value is not string)
        {
            throw new InvalidOperationException();
        }

        return Expression.Call(
            typeof(System.Console).GetMethod("WriteLine", new[] { typeof(string) })!,
            ExpressionOrConstant(value));
    }

    public static Expression[] WriteLineBefore(object value, Expression expression)
    {
        return new []
        {
            WriteLine(value),
            expression
        };
    }

    public static Expression[] WriteLineAfter(object value, Expression expression)
    {
        return new []
        {
            expression,
            WriteLine(value)
        };
    }

    public static BlockExpression ToBlock(this Expression[] arr) => Expression.Block(arr);

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

    public static MethodCallExpression GetTypeExpression(Expression obj) =>
        Expression.Call(obj, "GetType", Type.EmptyTypes);

    public static Expression DefaultValueOrTypeDefault(ParameterInfo x) => x.HasDefaultValue
        ? Expression.Constant(x.DefaultValue, x.ParameterType)
        : Expression.Default(x.ParameterType);

    public static Expression EqualExpression(Expression left, Expression right)
    {
        if (left.Type != right.Type)
            throw new InvalidOperationException(
                $"Left & Right Expression Types dont match ({left.Type}, {right.Type})");

        if (left.Type.IsPrimitive || left.Type == typeof(string))
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
