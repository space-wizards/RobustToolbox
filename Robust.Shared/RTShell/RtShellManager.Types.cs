using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using BindingFlags = System.Reflection.BindingFlags;

namespace Robust.Shared.RTShell;

public sealed partial class RtShellManager
{
    internal bool IsTransformableTo(Type left, Type right)
    {
        if (left.IsAssignableTo(right))
            return true;

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

    internal Expression GetTransformer(Type to, Type from, Expression input)
    {
        if (!IsTransformableTo(from, to))
            throw new InvalidCastException();

        if (to.IsGenericType && to.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            if (to.GenericTypeArguments[0] == from)
            {
                var tys = new [] {from};
                return Expression.Convert(
                    Expression.New(
                                typeof(UnitEnumerable<>).MakeGenericType(tys).GetConstructor(tys)!,
                                Expression.Convert(input, from)
                            ),
                        to
                    );
            }
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
