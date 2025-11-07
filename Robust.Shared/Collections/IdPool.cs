using System.Collections.Generic;
using Robust.Shared.Utility;

namespace Robust.Shared.Collections;

/// <summary>
/// Ported from Box2D
/// </summary>
internal sealed class IdPool
{
    private readonly List<int> _free;

    private int _nextIndex;

    public int Count => _nextIndex - _free.Count;

    public int Capacity => _nextIndex;

    internal IdPool()
    {
        _free = new List<int>();
    }

    public int AllocId()
    {
        var count = _free.Count;
        int id;

        if (count > 0)
        {
            id = _free.RemoveSwap(count - 1);
            return id;
        }

        id = _nextIndex;
        _nextIndex += 1;
        return id;
    }

    public void FreeId(int id)
    {
        DebugTools.Assert(_nextIndex > 0);
        DebugTools.Assert(0 <= id && id < _nextIndex);
        _free.Add(id);
    }
}
