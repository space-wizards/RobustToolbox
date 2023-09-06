using System;
using System.Collections.Generic;

namespace Robust.Shared.Toolshed.TypeParsers.Tuples;

public sealed class ValueTuple6TypeParser<T1, T2, T3, T4, T5, T6> : BaseTupleTypeParser<(T1, T2, T3, T4, T5, T6)>
{
    public override IEnumerable<Type> Fields => new[] {typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)};
    public override ValueTuple<T1, T2, T3, T4, T5, T6> Create(IReadOnlyList<object> values)
    {
        return new ValueTuple<T1, T2, T3, T4, T5, T6>((T1)values[0], (T2)values[1], (T3)values[2], (T4)values[3], (T5)values[4], (T6)values[5]);
    }
}