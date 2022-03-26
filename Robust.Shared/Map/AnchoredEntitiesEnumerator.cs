using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Map;

public struct AnchoredEntitiesEnumerator
{
    // ReSharper disable once CollectionNeverUpdated.Local
    private static readonly List<EntityUid> Dummy = new();
    public static readonly AnchoredEntitiesEnumerator Empty = new(Dummy.GetEnumerator());

    private List<EntityUid>.Enumerator _enumerator;

    internal AnchoredEntitiesEnumerator(List<EntityUid>.Enumerator enumerator)
    {
        _enumerator = enumerator;
    }

    public bool MoveNext([NotNullWhen(true)] out EntityUid? uid)
    {
        if (!_enumerator.MoveNext())
        {
            uid = null;
            return false;
        }

        uid = _enumerator.Current;
        return true;
    }
}
