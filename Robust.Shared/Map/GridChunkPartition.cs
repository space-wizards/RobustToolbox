using System.Collections.Generic;
using Robust.Shared.Maths;
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
            /*
            for (var i = rectangles.Count - 1; i >= 0; i--)
            {
                var box = rectangles[i];
                for (var j = i - 1; j >= 0; j--)
                {
                    var other = rectangles[j];

                    // Gone down as far as we can go.
                    if (other.Top < box.Bottom) break;

                    if (box.Left == other.Left && box.Right == other.Right)
                    {
                        box = new Box2i(box.Left, other.Bottom, box.Right, box.Top);
                        rectangles[i] = box;
                        rectangles.RemoveAt(j);
                        i -= 1;
                        continue;
                    }
                }
            }
            */

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
    }
}
