using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Server.GameObjects
{
    internal sealed class GridTileLookupChunk
    {
        internal const byte ChunkSize = 16;

        internal GridId GridId { get; }
        internal Vector2i Indices { get; }
        
        private GridTileLookupNode[,] _nodes = new GridTileLookupNode[ChunkSize,ChunkSize];
        
        internal GridTileLookupChunk(GridId gridId, Vector2i indices)
        {
            GridId = gridId;
            Indices = indices;

            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    _nodes[x, y] = new GridTileLookupNode(this, new Vector2i(Indices.X + x, Indices.Y + y));
                }
            }
        }

        internal GridTileLookupNode GetNode(Vector2i nodeIndices)
        {
            return _nodes[nodeIndices.X - Indices.X, nodeIndices.Y - Indices.Y];
        }
    }
}
