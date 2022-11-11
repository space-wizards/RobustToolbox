using System;
using System.Linq.Expressions;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.Shared.Serialization.Manager;

public sealed partial class SerializationManager
{
    //null values are the bane of my existence

    private static Expression ValueSafeCoalesceExpression(Expression left, Expression right)
    {
        return left.Type.IsValueType &&
               (!left.Type.IsGenericType || left.Type.GetGenericTypeDefinition() != typeof(Nullable<>))
            ? Expression.Condition(
                EqualExpression(left, Expression.Default(left.Type)),
                left,
                right)
            : Expression.Coalesce(left, right);
    }

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

    private T GetValueOrDefault<T>(object? value) where T : struct
    {
        return value as T? ?? default;
    }

    private bool IsNull(DataNode node)
    {
        return node is ValueDataNode valueDataNode && valueDataNode.Value.Trim().ToLower() is "null" or "";
    }

    public ValueDataNode NullNode() => new ValueDataNode("null");
}
