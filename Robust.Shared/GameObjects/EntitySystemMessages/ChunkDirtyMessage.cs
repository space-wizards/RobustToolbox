using System;
using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects.EntitySystemMessages
{
    [Serializable, NetSerializable]
    internal sealed class ChunkDirtyMessage : EntitySystemMessage
    {
        public Dictionary<MapId, Dictionary<GridId, List<Vector2i>>> DirtyChunks { get; }

        public ChunkDirtyMessage(Dictionary<MapId, Dictionary<GridId, List<Vector2i>>> chunks)
        {
            DirtyChunks = chunks;
        }
    }
}
