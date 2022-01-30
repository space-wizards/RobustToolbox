using System.Diagnostics.CodeAnalysis;
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
        _bottomLeft = ((viewPos - range) / chunkSize).Floored();
        _topRight = ((viewPos + range) / chunkSize).Ceiling();

        _x = _bottomLeft.X;
        _y = _topRight.Y;
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
