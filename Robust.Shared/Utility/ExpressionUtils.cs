using System.Linq.Expressions;

namespace Robust.Shared.Utility;

public static class ExpressionUtils
{
    public static MethodCallExpression WriteLine<T>(T value)
    {
        return Expression.Call(
            typeof(System.Console).GetMethod("WriteLine", new[] { typeof(T) })!,
            Expression.Constant(value));
    }

    public static BlockExpression WriteLineBefore<T>(T value, Expression expression)
    {
        return Expression.Block(
            WriteLine(value),
            expression);
    }
}
