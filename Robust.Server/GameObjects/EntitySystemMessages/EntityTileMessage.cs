using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.Server.GameObjects.EntitySystemMessages
{
    /// <summary>
    ///     Event for when the intersecting MapIndices of an entity changes.
    ///     Not called for space (given no tiles).
    /// </summary>
    /// <remarks>
    ///    List is empty if it's no longer intersecting any.
    /// </remarks>
    public sealed class TileLookupUpdateMessage : EntitySystemMessage
    {
        public Dictionary<GridId, List<MapIndices>>? NewIndices { get; }

        public TileLookupUpdateMessage(Dictionary<GridId, List<MapIndices>>? indices)
        {
            NewIndices = indices;
        }
    }
}