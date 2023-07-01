using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.Maths;

namespace Robust.Server.GameStates;

public struct ChunkIndicesEnumerator
{
    private Vector2i _bottomLeft;
    private Vector2i _topRight;

    private int _x;
    private int _y;

    public ChunkIndicesEnumerator(Vector2 viewPos, float range, float chunkSize)
    {
        var rangeVec = new Vector2(range, range);

        _bottomLeft = ((viewPos - rangeVec) / chunkSize).Floored();
        // Also floor this as we get the whole chunk anyway.
        _topRight = ((viewPos + rangeVec) / chunkSize).Floored();

        _x = _bottomLeft.X;
        _y = _bottomLeft.Y;
    }

    public bool MoveNext([NotNullWhen(true)] out Vector2i? chunkIndices)
    {
        if (_y > _topRight.Y)
        {
            _x++;
            _y = _bottomLeft.Y;
        }

        if (_x > _topRight.X)
        {
            chunkIndices = null;
            return false;
        }

        chunkIndices = new Vector2i(_x, _y);

        _y++;
        return true;
    }
}
