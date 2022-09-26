using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Map;

[Obsolete("EntityQuery for MapGridComponent instead")]
public struct GridEnumerator
{
    private IEnumerator<EntityUid> _enumerator;
    private EntityQuery<MapGridComponent> _query;

    internal GridEnumerator(IEnumerator<EntityUid> enumerator, EntityQuery<MapGridComponent> query)
    {
        _enumerator = enumerator;
        _query = query;
    }

    public bool MoveNext([NotNullWhen(true)] out IMapGrid? grid)
    {
        if (!_enumerator.MoveNext())
        {
            grid = null;
            return false;
        }

        grid = _query.GetComponent(_enumerator.Current).Grid;
        return true;
    }
}
