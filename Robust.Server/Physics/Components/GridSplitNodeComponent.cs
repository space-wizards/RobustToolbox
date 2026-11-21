using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.ViewVariables;

namespace Robust.Server.Physics.Components;

/// <summary>
/// Holds data for grid-split nodes so we can quickly check if a grid should split.
/// </summary>
[RegisterComponent]
public sealed partial class GridSplitNodeComponent : Component
{
    [ViewVariables]
    public readonly Dictionary<Vector2i, ChunkNodeGroup> Nodes = new();
}

public sealed class ChunkNodeGroup
{
    internal MapChunk Chunk = default!;
    public HashSet<ChunkSplitNode> Nodes = new();
}

public sealed class ChunkSplitNode
{
    public ChunkNodeGroup Group = default!;
    public List<Box2i> Indices { get; } = new();
    public HashSet<ChunkSplitNode> Neighbors { get; } = new();

    public int TileCount
    {
        get
        {
            var count = 0;

            foreach (var box in Indices)
            {
                count += box.Width * box.Height;
            }

            return count;
        }
    }

    public void AddIndex(Vector2i index)
    {
        Indices.Add(new Box2i(index.X, index.Y, index.X + 1, index.Y + 1));
    }

    public void CompactIndices()
    {
        if (Indices.Count <= 1)
            return;

        var tiles = new List<Vector2i>(TileCount);

        foreach (var index in GetTileIndices())
        {
            tiles.Add(index);
        }

        tiles.Sort((a, b) =>
        {
            var y = a.Y.CompareTo(b.Y);
            return y != 0 ? y : a.X.CompareTo(b.X);
        });

        Indices.Clear();
        var start = tiles[0];
        var previous = start;

        for (var i = 1; i < tiles.Count; i++)
        {
            var tile = tiles[i];

            if (tile.Y == previous.Y && tile.X == previous.X + 1)
            {
                previous = tile;
                continue;
            }

            Indices.Add(new Box2i(start.X, start.Y, previous.X + 1, previous.Y + 1));
            start = previous = tile;
        }

        Indices.Add(new Box2i(start.X, start.Y, previous.X + 1, previous.Y + 1));
    }

    public bool Contains(Vector2i index)
    {
        foreach (var box in Indices)
        {
            if (!box.ContainsTile(index))
                continue;

            return true;
        }

        return false;
    }

    public IEnumerable<Vector2i> GetTileIndices()
    {
        foreach (var box in Indices)
        {
            for (var x = box.Left; x < box.Right; x++)
            {
                for (var y = box.Bottom; y < box.Top; y++)
                {
                    yield return new Vector2i(x, y);
                }
            }
        }
    }

    public Vector2 GetCentre()
    {
        var centre = Vector2.Zero;
        var count = 0;

        foreach (var index in GetTileIndices())
        {
            centre += index;
            count++;
        }

        centre /= count;
        return centre;
    }
}
