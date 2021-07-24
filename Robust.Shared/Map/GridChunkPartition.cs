using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Robust.Shared.Map
{
    /// <summary>
    /// Partitions a grid chunk into a set of non-overlapping rectangles.
    /// </summary>
    internal static class GridChunkPartition
    {
        public static void PartitionChunk(IMapChunk chunk, out Box2i bounds)
        {
            var size = chunk.ChunkSize;

            // copy 2d img
            bool[,] image = new bool[size,size];

            for(ushort x=0;x<size;x++)
                for (ushort y = 0; y < size; y++)
                    image[x, y] = !chunk.GetTile(x, y).IsEmpty;

            Partition(size, size, image, out var blocks, out var blockCount);

            bounds = new Box2i();

            // convert blocks to rectangles array.
            for(int i=0;i< blockCount; i++)
            {
                var block = blocks[i];

                // block are in indices and rotated 90 degrees :(

                var left = block.y1;
                var right = block.y2 + 1;
                var bottom = block.x1;
                var top = block.x2 + 1;

                if(bounds.Size.Equals(Vector2i.Zero))
                    bounds = new Box2i(left, bottom, right, top);
                else
                    bounds = bounds.Union(new Box2i(left, bottom, right, top));
            }
        }

        private struct Block
        {
            public int x1;
            public int x2;
            public int y1;
            public int y2;
        }

        private static void Partition(in int L, in int W, in bool[,] img, out Block[] block, out int blockno)
        {
            // Credit: http://utopia.duth.gr/spiliot/papers/j7.pdf

            block = new Block[L*W];

            int[] p = new int[W];
            int[] c = new int[W];

            int kp = 0;
            blockno = 0;

            for (int y = 0; y < L; y++)
            {
                int x1 = 0;
                int x2 = 0;
                int kc = 0;
                bool intervalfound = false;
                int j_last = 0;
                int j_curr = 0;
                for (int x = 0; x < W; x++)
                {
                    bool try2match = false;
                    if (img[y,x] && !intervalfound)
                    {
                        intervalfound = true;
                        x1 = x;
                    }
                    if (!img[y,x] && intervalfound)
                    {
                        intervalfound = false;
                        x2 = x - 1;
                        try2match = true;
                    }
                    if (x == W - 1 && img[y,x] && intervalfound)
                    {
                        x2 = x;
                        try2match = true;
                    }
                    if (try2match)
                    {
                        bool intervalmatched = false;
                        for (int j = j_last; j < kp && x1 >= block[p[j]].x1; j++)
                            if (x1 == block[p[j]].x1 && x2 == block[p[j]].x2)
                            {
                                c[kc] = p[j];
                                block[p[j]].y2 = y;
                                intervalmatched = true;
                                j_curr = j;
                            }

                        j_last = j_curr;
                        if (!intervalmatched)
                        {
                            block[blockno].x1 = x1;
                            block[blockno].x2 = x2;
                            block[blockno].y1 = y;
                            block[blockno].y2 = y;
                            c[kc] = blockno++;
                        }

                        if (!intervalmatched)
                            kc++;
                    }
                }

                for (var i = 0; i < kc; i++)
                    p[i] = c[i];

                kp = kc;
            }
        }
    }
}
