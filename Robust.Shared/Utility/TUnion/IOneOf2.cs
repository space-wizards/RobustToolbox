using System;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Robust.Shared.Utility.TUnion;

[PublicAPI]
public interface IOneOf<T0, T1>
    where T0: notnull
    where T1: notnull
{
    /// <summary>
    ///     Returns true if and only if the stored value is Item1.
    /// </summary>
    public bool IsItem1 { get; }
    /// <summary>
    ///     Returns true if and only if the stored value is Item2.
    /// </summary>
    public bool IsItem2 { get; }

    // for Reasons related to C# jank, Item1 and Item2's nullable versions cannot work here.
    /// <summary>
    ///     Returns the first item.
    /// </summary>
    /// <exception cref="InvalidCastException">Thrown only if the value contained is not the first item.</exception>
    public T0 Item1OrErr { get; }
    /// <summary>
    ///     Returns the second item.
    /// </summary>
    /// <exception cref="InvalidCastException">Thrown only if the value contained is not the second item.</exception>
    public T1 Item2OrErr { get; }

    public T0 Expect1(FormattableString err);

    public T1 Expect2(FormattableString err);
}
