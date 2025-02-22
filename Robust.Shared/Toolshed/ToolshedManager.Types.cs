using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Robust.Shared.Toolshed.TypeParsers;

namespace Robust.Shared.Toolshed;

public sealed partial class ToolshedManager
{
    // If this gets updated, ensure that GetTransformer() is also updated
    internal bool IsTransformableTo(Type left, Type right)
    {
        if (left.IsAssignableToGeneric(right, this))
            return true;

        var asType = typeof(IAsType<>).MakeGenericType(right);

        if (left.GetInterfaces().Contains(asType))
        {
            return true;
        }

        if (!right.IsGenericType(typeof(IEnumerable<>)))
            return false;

        return right.GenericTypeArguments[0] == left;
    }

    // Autobots, roll out!
    // If this gets updated, ensure that IsTransformableTo() is also updated
    internal Expression GetTransformer(Type from, Type to, Expression input)
    {
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

        if (to.IsGenericType(typeof(IEnumerable<>)))
        {
            var toInner = to.GenericTypeArguments[0];
            var tys = new[] {toInner};
            return Expression.Convert(
                Expression.New(
                    typeof(UnitEnumerable<>).MakeGenericType(tys).GetConstructor(tys)!,
                    Expression.Convert(input, toInner)
                ),
                to
            );
        }

        if (from.IsAssignableToGeneric(to, this))
            return Expression.Convert(input, to);

        throw new InvalidCastException();
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
