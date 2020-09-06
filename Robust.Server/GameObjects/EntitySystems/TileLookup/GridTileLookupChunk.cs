using Robust.Shared.Map;

namespace Robust.Server.GameObjects.EntitySystems.TileLookup
{
    internal sealed class GridTileLookupChunk
    {
        internal const byte ChunkSize = 16;

        internal GridId GridId { get; }
        internal MapIndices Indices { get; }
        
        private GridTileLookupNode[,] _nodes = new GridTileLookupNode[ChunkSize,ChunkSize];
        
        internal GridTileLookupChunk(GridId gridId, MapIndices indices)
        {
            GridId = gridId;
            Indices = indices;

            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    _nodes[x, y] = new GridTileLookupNode(this, new MapIndices(Indices.X + x, Indices.Y + y));
                }
            }
        }

        internal GridTileLookupNode GetNode(MapIndices nodeIndices)
        {
            return _nodes[nodeIndices.X - Indices.X, nodeIndices.Y - Indices.Y];
        }
    }
}