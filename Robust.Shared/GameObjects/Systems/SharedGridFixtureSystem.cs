using Robust.Shared.Map;

namespace Robust.Shared.GameObjects
{
    public abstract class SharedGridFixtureSystem : EntitySystem
    {
        internal string GetChunkId(MapChunk chunk)
        {
            return $"grid_chunk-{chunk.Indices.X}-{chunk.Indices.Y}";
        }
    }
}
