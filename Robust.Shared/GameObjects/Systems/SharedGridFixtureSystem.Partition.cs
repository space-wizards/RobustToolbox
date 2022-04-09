using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedGridFixtureSystem
{
    [Dependency] private readonly IVerticesSimplifier _simp = default!;

    /// <summary>
    /// Iterates through a chunk row and tries to find the first valid polygon tile.
    /// </summary>
    private static void GetRowPolygons(List<List<Vector2i>> polys, ushort row, MapChunk chunk)
    {
        var running = false;
        ushort origin = default;

        for (ushort x = 0; x < chunk.ChunkSize; x++)
        {
            var tile = chunk.GetTile(x, row);
            var nextTile = x == chunk.ChunkSize - 1 ? Tile.Empty : chunk.GetTile((ushort) (x + 1), row);

            if (!running)
            {
                if (!CanStart(tile)) continue;

                running = true;
                origin = x;
            }

            if (!TryEnd(tile, nextTile)) continue;

            // TODO: Test ideas
            // Spawn same diagonal repeatedly, should create new fixtures.

            running = false;
            var polygon = new List<Vector2i>(4);

            var originTile = chunk.GetTile(origin, row);
            var originCollision = originTile.Flags;

            var endTile = chunk.GetTile(x, row);
            var endCollision = endTile.Flags;

            // Grab:
            // Origin bot left
            // End bot right
            // End top right
            // Origin top left
            polygon.Add(GetVertex(originCollision, 0, origin, row));
            polygon.Add(GetVertex(endCollision, 1, x, row));
            polygon.Add(GetVertex(endCollision, 2, x, row));
            polygon.Add(GetVertex(originCollision, 3, origin, row));

            // De-dupe verts: Should only ever be 1 duplicated for triangles.
            for (var i = 0; i < polygon.Count; i++)
            {
                var vert = polygon[i];
                var nextVert = polygon[(i + 1) % polygon.Count];
                if (vert.Equals(nextVert))
                {
                    polygon.RemoveAt(i);
                    break;
                }
            }

            polys.Add(polygon);
        }
    }

    private static bool CanStart(Tile tile)
    {
        return !tile.IsEmpty;
    }

    private static bool TryEnd(Tile tile, Tile nextTile)
    {
        if (nextTile.IsEmpty)
            return true;

        // Tries to terminate the tile.
        switch (nextTile.Flags)
        {
            case TileFlag.None:
            case TileFlag.BottomLeft:
            case TileFlag.TopLeft:
                break;
            case TileFlag.BottomRight:
            case TileFlag.TopRight:
                return true;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // Okay so next tile isn't a terminator, check if this one is.
        switch (tile.Flags)
        {
            case TileFlag.BottomLeft:
            case TileFlag.TopLeft:
                return true;
            case TileFlag.None:
            case TileFlag.BottomRight:
            case TileFlag.TopRight:
                return false;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Tries to get a vertex with the specified index.
    /// </summary>
    /// <param name="flag"></param>
    /// <param name="index"></param>
    /// <param name="x">Left index of the tile</param>
    /// <param name="y">Bottom index of the tile</param>
    /// <returns></returns>
    private static Vector2i GetVertex(TileFlag flag, int index, int x, int y)
    {
        return index switch
        {
            0 => flag switch
            {
                TileFlag.None => new Vector2i(x, y),
                TileFlag.BottomLeft => new Vector2i(x, y),
                TileFlag.BottomRight => new Vector2i(x, y),
                TileFlag.TopRight => new Vector2i(x + 1, y),
                TileFlag.TopLeft => new Vector2i(x, y),
                _ => throw new NotImplementedException()
            },
            1 => flag switch
            {
                TileFlag.None => new Vector2i(x + 1, y),
                TileFlag.BottomLeft => new Vector2i(x + 1, y),
                TileFlag.BottomRight => new Vector2i(x + 1, y),
                TileFlag.TopRight => new Vector2i(x + 1, y),
                TileFlag.TopLeft => new Vector2i(x, y),
                _ => throw new NotImplementedException()
            },
            2 => flag switch
            {
                TileFlag.None => new Vector2i(x + 1, y + 1),
                TileFlag.BottomLeft => new Vector2i(x, y + 1),
                TileFlag.BottomRight => new Vector2i(x + 1, y + 1),
                TileFlag.TopRight => new Vector2i(x + 1, y + 1),
                TileFlag.TopLeft => new Vector2i(x + 1, y + 1),
                _ => throw new NotImplementedException()
            },
            3 => flag switch
            {
                TileFlag.None => new Vector2i(x, y + 1),
                TileFlag.BottomLeft => new Vector2i(x, y + 1),
                TileFlag.BottomRight => new Vector2i(x + 1, y + 1),
                TileFlag.TopRight => new Vector2i(x, y + 1),
                TileFlag.TopLeft => new Vector2i(x, y + 1),
                _ => throw new NotImplementedException()
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    internal void PartitionChunk(MapChunk chunk, out Box2i bounds, out List<List<Vector2i>> polygons)
    {
        polygons = new List<List<Vector2i>>();

        for (ushort y = 0; y < chunk.ChunkSize; y++)
        {
            GetRowPolygons(polygons, y, chunk);
        }

        // Patch them together as available (TODO NOW
        PatchPolygons(polygons);

        bounds = new Box2i();
        var minimum = Vector2i.Zero;
        var maximum = Vector2i.Zero;

        foreach (var poly in polygons)
        {
            foreach (var vert in poly)
            {
                minimum = Vector2i.ComponentMin(minimum, vert);
                maximum = Vector2i.ComponentMax(maximum, vert);
            }
        }

        bounds = new Box2i(minimum, maximum);
    }

    /// <summary>
    /// Attempt to combine polygons together to reduce their count.
    /// </summary>
    private void PatchPolygons(List<List<Vector2i>> polygons)
    {
        var combined = true;

        while (combined)
        {
            combined = false;

            for (var i = polygons.Count - 1; i >= 0; i--)
            {
                var polyA = polygons[i];

                for (var j = i - 1; j >= 0; j--)
                {
                    var polyB = polygons[j];

                    var dupe = 0;
                    foreach (var vertA in polyA)
                    {
                        foreach (var vertB in polyB)
                        {
                            if (!vertA.Equals(vertB)) continue;
                            dupe++;
                        }
                    }

                    if (dupe < 2) continue;

                    if (!TryCombinePolygons(polyA, polyB, out var combinedPoly) ||
                        !IsConvex(combinedPoly)) continue;

                    // Replace polyA and remove polyB.
                    polygons[i] = combinedPoly;
                    polygons.RemoveAt(j);
                    combined = true;
                }
            }
        }
    }

    private bool TryCombinePolygons(List<Vector2i> polyA, List<Vector2i> polyB, [NotNullWhen(true)] out List<Vector2i>? list)
    {
        var combined = new List<Vector2>(polyA.Count + polyB.Count - 2);
        var spliced = false;

        // Need to insert polyB into polyA's verts
        for (var i = 0; i < polyA.Count; i++)
        {
            var vertA = polyA[i];
            combined.Add(vertA);

            if (spliced) continue;

            for (var j = 0; j < polyB.Count; j++)
            {
                var vertB = polyB[j];
                if (!vertB.Equals(vertA)) continue;

                spliced = true;
                // Alright common vert time to slice
                for (var x = 1; x < polyB.Count - 1; x++)
                {
                    var idx = (j + x) % polyB.Count;
                    combined.Add(polyB[idx]);
                }
            }
        }

        combined = _simp.Simplify(combined, 0f);

        Span<Vector2> spin = stackalloc Vector2[combined.Count];
        var index = 0;

        foreach (var vert in combined)
        {
            spin[index] = vert;
            index++;
        }

        list = new List<Vector2i>(combined.Count);

        foreach (var a in combined)
            list.Add((Vector2i) a);

        return true;
    }

    private bool IsConvex(List<Vector2i> polygon)
    {
        // Also considering max verts allowed in box2d.
        if (polygon.Count < 3 || polygon.Count > 8) return false;
        // https://stackoverflow.com/questions/471962/how-do-i-efficiently-determine-if-a-polygon-is-convex-non-convex-or-complex/45372025#45372025

        var (oldX, oldY) = polygon[^2];
        var (newX, newY) = polygon[^1];
        var newDirection = MathF.Atan2(newY - oldY, newX - oldX);
        var oldDirection = 0f;
        float orientation = 0f;
        var angleSum = 0f;

        for (var i = 0; i < polygon.Count; i++)
        {
            var newPoint = polygon[i];
            oldX = newX;
            oldY = newY;
            oldDirection = newDirection;
            (newX, newY) = newPoint;
            newDirection = MathF.Atan2(newY - oldY, newX - oldX);

            if (oldX.Equals(newX) && oldY.Equals(newY))
                return false;

            var angle = newDirection - oldDirection;
            if (angle <= -MathF.PI)
                angle += MathF.Tau;
            else if (angle > MathF.PI)
                angle -= MathF.Tau;
            if (i == 0)
            {
                if (angle == 0f)
                    return false;

                orientation = angle > 0f ? 1f : -1f;
            }
            else
            {
                if (orientation * angle < 0f)
                    return false;
            }

            angleSum += angle;
        }

        return MathF.Abs(MathF.Round(angleSum / MathF.Tau)).Equals(1f);
    }
}
