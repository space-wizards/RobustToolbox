using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Robust.Shared.Utility.TUnion;

/// <summary>
/// A compact tagged union type that can store any value.
/// </summary>
/// <remarks>This takes up the size of T0 + T1 + 4, and is NOT a true union internally. Prefer OneOfValue when possible.</remarks>
/// <typeparam name="T0">The type of Item1OrErr</typeparam>
/// <typeparam name="T1">The type of Item2OrErr</typeparam>
[PublicAPI]
public readonly struct OneOf<T0, T1> : IOneOf<T0, T1>
    where T0: notnull
    where T1: notnull
{
    private readonly int _kind;
    private readonly T0? _item1;
    private readonly T1? _item2;

    /// <summary>
    ///     Initialize the OneOf as the first item.
    /// </summary>
    /// <param name="val">Value to store.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OneOf(T0 val)
    {
        _kind = 0;
        _item1 = val;
    }

    /// <summary>
    ///     Initialize the OneOf as the second item.
    /// </summary>
    /// <param name="val">Value to store.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OneOf(T1 val)
    {
        _kind = 1;
        _item2 = val;
    }

    /// <inheritdoc/>
    public bool IsItem1 => _kind == 0;
    /// <inheritdoc/>
    public bool IsItem2 => _kind == 1;

    public T0? Item1 => IsItem1 ? _item1 : default;
    public T1? Item2 => IsItem2 ? _item2 : default;


    /// <inheritdoc/>
    public T0 Item1OrErr => Item1 ?? throw new InvalidCastException("Cannot cast to Item1, wrong kind.");
    /// <inheritdoc/>
    public T1 Item2OrErr => Item2 ?? throw new InvalidCastException("Cannot cast to Item2, wrong kind.");

    /// <inheritdoc/>
    public T0 Expect1(FormattableString err)
    {
        if (Item1 is { } item)
        {
            return item;
        }

        throw new Exception(err.ToString());
    }

    /// <inheritdoc/>
    public T1 Expect2(FormattableString err)
    {
        if (Item2 is { } item)
        {
            return item;
        }

        throw new Exception(err.ToString());
    }
}
