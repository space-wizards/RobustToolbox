using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Map;

[Obsolete("EntityQuery for MapGridComponent instead")]
public struct GridEnumerator
{
    private Dictionary<GridId, EntityUid>.Enumerator _enumerator;
    private EntityQuery<MapGridComponent> _query;

    internal GridEnumerator(Dictionary<GridId, EntityUid>.Enumerator enumerator, EntityQuery<MapGridComponent> query)
    {
        _enumerator = enumerator;
        _query = query;
    }

    public bool MoveNext([NotNullWhen(true)] out MapGridComponent? grid)
    {
        if (!_enumerator.MoveNext())
        {
            grid = null;
            return false;
        }

        var (_, uid) = _enumerator.Current;

        grid = _query.GetComponent(uid);
        return true;
    }
}
