using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;

namespace Robust.Server.GameStates;

public struct ChunkIndicesEnumerator
{
    private Vector2i _topLeft;
    private Vector2i _bottomRight;

    private int _x;
    private int _y;

    public ChunkIndicesEnumerator(Box2 viewBox, float chunkSize)
    {
        _topLeft = (viewBox.TopLeft / chunkSize).Floored();
        _bottomRight = (viewBox.BottomRight / chunkSize).Floored();

        _x = _topLeft.X;
        _y = _bottomRight.Y;
    }

    public bool MoveNext([NotNullWhen(true)] out Vector2i? chunkIndices)
    {
        if (_y > _topLeft.Y)
        {
            _x++;
            _y = _bottomRight.Y;
        }

        if (_x > _bottomRight.X)
        {
            chunkIndices = null;
            return false;
        }

        chunkIndices = new Vector2i(_x, _y);

        _y++;
        return true;
    }
}
