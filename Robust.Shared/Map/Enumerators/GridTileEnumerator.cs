using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Map.Enumerators;

/// <summary>
/// Returns all tiles on a grid.
/// </summary>
public struct GridTileEnumerator
{
    private readonly EntityUid _gridUid;
    private Dictionary<Vector2i, MapChunk>.Enumerator _chunkEnumerator;
    private readonly ushort _chunkSize;
    private int _index;
    private readonly bool _ignoreEmpty;

    internal GridTileEnumerator(EntityUid gridUid, Dictionary<Vector2i, MapChunk>.Enumerator chunkEnumerator, ushort chunkSize, bool ignoreEmpty)
    {
        _gridUid = gridUid;
        _chunkEnumerator = chunkEnumerator;
        _chunkSize = chunkSize;
        _index = _chunkSize * _chunkSize;
        _ignoreEmpty = ignoreEmpty;
    }

    public bool MoveNext([NotNullWhen(true)] out TileRef? tileRef)
    {
        while (true)
        {
            if (_index == _chunkSize * _chunkSize)
            {
                if (!_chunkEnumerator.MoveNext())
                {
                    tileRef = null;
                    return false;
                }

                _index = 0;
            }

            var (chunkOrigin, chunk) = _chunkEnumerator.Current;
            DebugTools.Assert(chunk.FilledTiles > 0, $"Encountered empty chunk while enumerating tiles");
            var x = (ushort) (_index / _chunkSize);
            var y = (ushort) (_index % _chunkSize);
            var tile = chunk.GetTile(x, y);
            _index++;

            if (_ignoreEmpty && tile.IsEmpty)
                continue;

            var gridX = x + chunkOrigin.X * _chunkSize;
            var gridY = y + chunkOrigin.Y * _chunkSize;
            tileRef = new TileRef(_gridUid, gridX, gridY, tile);
            return true;
        }
    }
}
