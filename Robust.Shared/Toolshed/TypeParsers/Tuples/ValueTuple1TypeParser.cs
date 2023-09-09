using System;
using System.Collections.Generic;

namespace Robust.Shared.Toolshed.TypeParsers.Tuples;

public sealed class ValueTuple1TypeParser<T1> : BaseTupleTypeParser<ValueTuple<T1>>
{
    public override IEnumerable<Type> Fields => new[] {typeof(T1)};
    public override ValueTuple<T1> Create(IReadOnlyList<object> values)
    {
        return new ValueTuple<T1>((T1)values[0]);
    }
}