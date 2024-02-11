using System;
using System.Collections.Generic;

namespace Robust.Shared.Toolshed.TypeParsers.Tuples;

public sealed class ValueTuple8TypeParser<T1, T2, T3, T4, T5, T6, T7, T8> : BaseTupleTypeParser<ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>>
    where T8: unmanaged
{
    public override IEnumerable<Type> Fields => new[] {typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8)};
    public override ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8> Create(IReadOnlyList<object> values)
    {
        return new ValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>((T1)values[0], (T2)values[1], (T3)values[2], (T4)values[3], (T5)values[4], (T6)values[5], (T7)values[6], (T8)values[7]);
    }
}