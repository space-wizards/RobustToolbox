using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed;

public sealed partial class ToolshedManager
{
    internal bool IsTransformableTo(Type left, Type right)
    {
        if (left.IsAssignableToGeneric(right, this))
            return true;

        var asType = typeof(IAsType<>).MakeGenericType(right);

        if (left.GetInterfaces().Contains(asType))
        {
            return true;
        }

        if (right == typeof(object))
            return true; // May need boxed.

        if (right.IsGenericType && right.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            if (right.GenericTypeArguments[0] == left)
                return true;

            return false;
        }

        return false;
    }

    internal Expression GetTransformer(Type from, Type to, Expression input)
    {
        if (!IsTransformableTo(from, to))
            throw new InvalidCastException();

        if (from.IsAssignableTo(to))
            return Expression.Convert(input, to);

        var asType = typeof(IAsType<>).MakeGenericType(to);

        if (from.GetInterfaces().Contains(asType))
        {
            // Just call astype 4head
            return Expression.Convert(
                    Expression.Call(input, asType.GetMethod(nameof(IAsType<int>.AsType))!),
                    to
                );
        }

        if (to.IsGenericType && to.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var toInner = to.GenericTypeArguments[0];
            var tys = new [] {toInner};
            return Expression.Convert(
                Expression.New(
                            typeof(UnitEnumerable<>).MakeGenericType(tys).GetConstructor(tys)!,
                            Expression.Convert(input, toInner)
                        ),
                    to
                );
        }

        return Expression.Convert(input, to);
    }
}

internal sealed record UnitEnumerable<T>(T Value) : IEnumerable<T>
{
    internal record struct UnitEnumerator(T Value) : IEnumerator<T>
    {
        private bool _taken = false;

        public bool MoveNext()
        {
            if (_taken)
                return false;
            _taken = true;
            return true;
        }

        public void Reset()
        {
            _taken = false;
        }

        public T Current => Value;

        object IEnumerator.Current => Current!;

        public void Dispose()
        {
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return new UnitEnumerator(Value);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
