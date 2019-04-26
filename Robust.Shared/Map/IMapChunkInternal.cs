using Robust.Shared.Timing;

namespace Robust.Shared.Map
{
    internal interface IMapChunkInternal : IMapChunk
    {
        GameTick LastModifiedTick { get; }
    }
}
