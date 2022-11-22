using System;
using System.Diagnostics;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.Manager;

public sealed partial class SerializationManager
{
    //null values are the bane of my existence ~<paul

    public static Expression GetNullExpression(Expression managerConst, Type type)
    {
        return type.IsValueType
            ? Expression.Call(managerConst, nameof(GetNullable), new[] { type })
            : Expression.Constant(null, type);
    }

    private T? GetNullable<T>() where T : struct
    {
        return null;
    }

    public static Expression WrapNullableIfNeededExpression(Expression expr, bool nullable)
    {
        if (nullable && expr.Type.IsValueType && !expr.Type.IsNullable())
            return Expression.New(expr.Type.EnsureNullableType().GetConstructor(new[] { expr.Type })!, expr);
        return expr;
    }

    private T GetValueOrDefault<T>(object? value) where T : struct
    {
        return value as T? ?? default;
    }

    public static bool IsNull(DataNode node)
    {
        return node.IsNull;
    }

    public ValueDataNode NullNode() => ValueDataNode.Null();

    public static Expression StructNullHasValue(Expression valueExpression)
    {
        Debug.Assert(valueExpression.Type.IsValueType);
        Debug.Assert(valueExpression.Type.IsNullable());
        return Expression.Property(valueExpression, "HasValue");
    }
}
