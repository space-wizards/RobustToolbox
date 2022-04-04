using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    /// <summary>
    /// Partitions a grid chunk into a set of non-overlapping rectangles.
    /// </summary>
    internal static class GridChunkPartition
    {
        /// <summary>
        /// Iterates through a chunk row and tries to find the first valid polygon tile.
        /// </summary>
        private static void GetRowPolygons(List<List<Vector2i>> polys, ushort row, MapChunk chunk)
        {
            var running = false;
            ushort origin = default;
            //var polys = new List<List<Vector2i>>();
            //var fakePolys = new List<Box2i>();

            for (ushort x = 0; x < chunk.ChunkSize; x++)
            {
                var tile = chunk.GetTile(x, row);

                if (!running)
                {
                    if (!CanStart(tile)) continue;

                    running = true;
                    origin = x;
                    continue;
                }

                if (!TryEnd(tile, out var index) && x != chunk.ChunkSize - 1) continue;

                running = false;
                var polygon = new List<Vector2i>();
                var endIndex = (ushort) (index + x);

                var originTile = chunk.GetTile(origin, row);
                var endTile = chunk.GetTile(endIndex, row);

                // Grab:
                // Origin bot left
                // End bot right
                // End top right
                // Origin top left
                // TODO: Diagonals you numpty
                polygon.Add(new Vector2i(origin, row));
                polygon.Add(new Vector2i(endIndex + 1, row));
                polygon.Add(new Vector2i(endIndex + 1, row + 1));
                polygon.Add(new Vector2i(origin, row + 1));

                polys.Add(polygon);
            }
        }

        private static bool CanStart(Tile tile)
        {
            if (tile.IsEmpty) return false;

            return true;
        }

        private static bool TryEnd(Tile tile, out int index)
        {
            // Tries to terminate the tile.
            // If the tile is empty will return the preceding tile as the terminator
            if (tile.IsEmpty)
            {
                index = -1;
                return true;
            }

            index = 0;
            return false;
        }

        public static void PartitionChunk(MapChunk chunk, out Box2i bounds, out List<List<Vector2i>> polygons)
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
        private static void PatchPolygons(List<List<Vector2i>> polygons)
        {
            // TODO: If any angle > 180 degrees I think?

            // TODO: Iterate every polygon and check all forward ones to see if any 2 points match
            // Recursively do this until they're all combined.

            var combined = true;

            while (combined)
            {
                combined = false;

                for (var i = 0; i < polygons.Count; i++)
                {
                    var polyA = polygons[i];
                    var end = polygons.Count;

                    for (var j = i; j < end; j++)
                    {
                        var polyB = polygons[j];

                        var dupe = 0;
                        // TODO: Check if any 2 points align, then check if that poly is convex.
                        foreach (var vertA in polyA)
                        {
                            foreach (var vertB in polyB)
                            {
                                if (!vertA.Equals(vertB)) continue;
                                dupe++;
                            }
                        }

                        if (dupe < 2) continue;

                        var combinedPoly = CombinePolygons(polyA, polyB);

                        if (!IsConvex(combinedPoly)) continue;

                        // Replace polyA and remove polyB.
                        polygons[i] = combinedPoly;
                        polygons.RemoveSwap(j);
                        j--;
                        end--;
                        combined = true;
                    }
                }
            }
        }

        private static List<Vector2i> CombinePolygons(List<Vector2i> polyA, List<Vector2i> polyB)
        {
            // TODO: Need a test for this real bad.
            // TODO: Lord this is disgusting.
            var set = new HashSet<Vector2i>(polyA);
            set.EnsureCapacity(polyA.Count + polyB.Count - 2);
            set.UnionWith(polyB);

            Span<Vector2> spin = stackalloc Vector2[set.Count];
            var index = 0;

            foreach (var vert in set)
            {
                spin[index] = vert;
                index++;
            }

            var wrapped = GiftWrap.SetConvexHull(spin, spin.Length);
            var list = new List<Vector2i>(wrapped.Length);

            foreach (var vec in wrapped)
                list.Add((Vector2i) vec);

            return list;
        }

        private static bool IsConvex(List<Vector2i> polygon)
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
}
