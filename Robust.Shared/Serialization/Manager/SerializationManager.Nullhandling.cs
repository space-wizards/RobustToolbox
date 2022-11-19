using System;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager;

public sealed partial class SerializationManager
{
    //null values are the bane of my existence
    //todo paul expand our testing of null-handling in serv3

    private static Expression GetNullExpression(Expression managerConst, Type type)
    {
        return type.IsValueType
            ? Expression.Call(managerConst, nameof(GetNullable), new[] { type })
            : Expression.Constant(null, type);
    }

    private T? GetNullable<T>() where T : struct
    {
        return null;
    }

    public static Expression WrapNullableIfNeededExpression(Expression expr, bool nullable, Type type)
    {
        if (nullable && type.IsValueType && !expr.Type.IsNullable())
            return Expression.New(type.EnsureNullableType().GetConstructor(new[] { type })!, expr);
        return expr;
    }

    private T GetValueOrDefault<T>(object? value) where T : struct
    {
        return value as T? ?? default;
    }

    private bool IsNull(DataNode node)
    {
        return node.IsNull;
    }

    public ValueDataNode NullNode() => ValueDataNode.Null();
}
