using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Map.Enumerators;

public struct AnchoredEntitiesEnumerator : IEnumerable<EntityUid>, IEnumerator<EntityUid>
{
    // ReSharper disable once CollectionNeverUpdated.Local
    private static readonly List<EntityUid> Dummy = new();
    public static readonly AnchoredEntitiesEnumerator Empty = new(Dummy.GetEnumerator());

    private List<EntityUid>.Enumerator _enumerator;
    private readonly bool _valid;

    internal AnchoredEntitiesEnumerator(List<EntityUid>.Enumerator enumerator)
    {
        _enumerator = enumerator;
        _valid = true;
    }

    public readonly AnchoredEntitiesEnumerator GetEnumerator()
    {
        return this;
    }

    public readonly EntityUid Current => _enumerator.Current;

    readonly object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (!_valid)
            return false;

        return _enumerator.MoveNext();
    }

    public bool MoveNext([NotNullWhen(true)] out EntityUid? uid)
    {
        if (!MoveNext())
        {
            uid = null;
            return false;
        }

        uid = Current;
        return true;
    }

    public void Dispose()
    {
        _enumerator.Dispose();
    }

    readonly IEnumerator<EntityUid> IEnumerable<EntityUid>.GetEnumerator()
    {
        return GetEnumerator();
    }

    readonly IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Reset()
    {
        throw new NotSupportedException();
    }
}
