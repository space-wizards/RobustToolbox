using System;
using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Robust.Shared.Map
{
    /// <summary>
    /// Partitions a grid chunk into a set of non-overlapping rectangles.
    /// </summary>
    internal static class GridChunkPartition
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="bounds">The overall bounds that covers every rectangle.</param>
        /// <param name="rectangles">Each individual rectangle comprising the chunk's bounds</param>
        public static void PartitionChunk(IMapChunk chunk, out Box2i bounds, out List<Box2i> rectangles)
        {
            rectangles = new List<Box2i>();

            // TODO: Use the existing PartitionChunk version because that one is likely faster and you can Span that shit.
            // Convert each line into boxes as long as they can be.
            for (ushort y = 0; y < chunk.ChunkSize; y++)
            {
                var origin = 0;
                var running = false;

                for (ushort x = 0; x < chunk.ChunkSize; x++)
                {
                    if (!chunk.GetTile(x, y).IsEmpty)
                    {
                        running = true;
                        continue;
                    }

                    // Still empty
                    if (running)
                    {
                        rectangles.Add(new Box2i(origin, y, x, y + 1));
                    }

                    origin = x + 1;
                    running = false;
                }

                if (running)
                    rectangles.Add(new Box2i(origin, y, chunk.ChunkSize, y + 1));
            }

            // Patch them together as available
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

            bounds = new Box2i();

            foreach (var rectangle in rectangles)
            {
                bounds = bounds.IsEmpty() ? rectangle : bounds.Union(rectangle);
            }
        }
    }
}
