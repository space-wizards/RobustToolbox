using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Shared.Map.Events;

/// <summary>
/// Raised directed on a grid to get its bounds.
/// </summary>
/// <remarks>
/// Really this exists to get around test dependency creeping.
/// </remarks>
[ByRefEvent]
internal readonly record struct RegenerateGridBoundsEvent(EntityUid Entity, Dictionary<MapChunk, List<Box2i>> ChunkRectangles, List<MapChunk> RemovedChunks)
{
    public readonly EntityUid Entity = Entity;

    public readonly Dictionary<MapChunk, List<Box2i>> ChunkRectangles = ChunkRectangles;

    public readonly List<MapChunk> RemovedChunks = RemovedChunks;
}
