using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.Collections;
using Robust.Shared.Maths;

namespace Robust.Shared.Map.Enumerators;

/// <summary>
/// Iterates chunk indices but prefers ones closer towards the center first.
/// </summary>
public record struct NearestChunkEnumerator
{
    private readonly Vector2i _chunkLB;
    private readonly Vector2i _chunkRT;

    private ValueList<Vector2i> _chunks = new();

    private int _n;

    public NearestChunkEnumerator(Box2 localAABB, int chunkSize)
    {
        _chunkLB = (localAABB.BottomLeft / chunkSize).Floored();
        _chunkRT = (localAABB.TopRight / chunkSize).Floored();
        InitializeChunks(new Vector2i(chunkSize, chunkSize));
    }

    public NearestChunkEnumerator(Box2 localAABB, Vector2i chunkSize)
    {
        _chunkLB = (localAABB.BottomLeft / chunkSize).Floored();
        _chunkRT = (localAABB.TopRight / chunkSize).Floored();
        InitializeChunks(chunkSize);
    }

    private void InitializeChunks(Vector2i chunkSize)
    {
        var bl = (Vector2)_chunkLB * chunkSize;
        var tr = (Vector2)_chunkRT * chunkSize;
        var halfChunk = new Vector2(chunkSize.X / 2f, chunkSize.Y / 2f);

        var center = (tr - bl) / 2 + bl;

        for (var x = _chunkLB.X; x < _chunkRT.X; x++)
        {
            for (var y = _chunkLB.Y; y < _chunkRT.Y; y++)
            {
                _chunks.Add(new Vector2i(x, y) * chunkSize);
            }
        }

        _chunks.Sort((c1, c2) => ((c1 + halfChunk) - center).LengthSquared().CompareTo(((c2 + halfChunk) - center).LengthSquared()));
    }

    public bool MoveNext([NotNullWhen(true)] out Vector2i? indices)
    {
        if (_n >= _chunks.Count)
        {
            indices = null;
            return false;
        }

        indices = _chunks[_n++] ;
        return true;
    }
}
