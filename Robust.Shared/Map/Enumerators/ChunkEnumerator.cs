using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;

namespace Robust.Shared.Map.Enumerators;

internal struct ChunkEnumerator
{
    private Dictionary<Vector2i, MapChunk> _chunks;
    private Vector2i _chunkLB;
    private Vector2i _chunkRT;

    private int _xIndex;
    private int _yIndex;

    internal ChunkEnumerator(Dictionary<Vector2i, MapChunk> chunks, Box2 localAABB, int chunkSize)
    {
        _chunks = chunks;

        _chunkLB = new Vector2i((int)Math.Floor(localAABB.Left / chunkSize), (int)Math.Floor(localAABB.Bottom / chunkSize));
        _chunkRT = new Vector2i((int)Math.Floor(localAABB.Right / chunkSize), (int)Math.Floor(localAABB.Top / chunkSize));

        _xIndex = _chunkLB.X;
        _yIndex = _chunkLB.Y;
    }

    public bool MoveNext([NotNullWhen(true)] out MapChunk? chunk)
    {
        if (_yIndex > _chunkRT.Y)
        {
            _yIndex = _chunkLB.Y;
            _xIndex += 1;
        }

        for (var x = _xIndex; x <= _chunkRT.X; x++)
        {
            for (var y = _yIndex; y <= _chunkRT.Y; y++)
            {
                var gridChunk = new Vector2i(x, y);
                if (!_chunks.TryGetValue(gridChunk, out chunk)) continue;
                _xIndex = x;
                _yIndex = y + 1;
                return true;
            }

            _yIndex = _chunkLB.Y;
        }

        chunk = null;
        return false;
    }
}
