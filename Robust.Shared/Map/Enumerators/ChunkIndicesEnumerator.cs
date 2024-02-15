using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.Maths;

namespace Robust.Shared.Map.Enumerators;

/// <summary>
/// Generic iterator for chunk indices for the specified bounds with the specified chunk size.
/// </summary>
public struct ChunkIndicesEnumerator
{
    private readonly Vector2i _chunkLB;
    private readonly Vector2i _chunkRT;

    private int _xIndex;
    private int _yIndex;

    public ChunkIndicesEnumerator(Vector2 viewPos, float range, float chunkSize)
    {
        var rangeVec = new Vector2(range, range);

        _chunkLB = ((viewPos - rangeVec) / chunkSize).Floored();
        // Also floor this as we get the whole chunk anyway.
        _chunkRT = ((viewPos + rangeVec) / chunkSize).Floored();

        _xIndex = _chunkLB.X;
        _yIndex = _chunkLB.Y;
    }

    public ChunkIndicesEnumerator(Box2 localAABB, int chunkSize)
    {
        _chunkLB = new Vector2i((int)Math.Floor(localAABB.Left / chunkSize), (int)Math.Floor(localAABB.Bottom / chunkSize));
        _chunkRT = new Vector2i((int)Math.Floor(localAABB.Right / chunkSize), (int)Math.Floor(localAABB.Top / chunkSize));

        _xIndex = _chunkLB.X;
        _yIndex = _chunkLB.Y;
    }

    public bool MoveNext([NotNullWhen(true)] out Vector2i? indices)
    {
        if (_yIndex > _chunkRT.Y)
        {
            _yIndex = _chunkLB.Y;
            _xIndex++;
        }

        if (_xIndex > _chunkRT.X)
        {
            indices = null;
            return false;
        }

        indices = new Vector2i(_xIndex, _yIndex);
        _yIndex++;

        return true;
    }
}
