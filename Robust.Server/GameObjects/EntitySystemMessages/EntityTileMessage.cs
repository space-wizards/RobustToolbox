using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     Event for when the intersecting Vector2i of an entity changes.
    ///     Not called for space (given no tiles).
    /// </summary>
    /// <remarks>
    ///    List is empty if it's no longer intersecting any.
    /// </remarks>
    public sealed class TileLookupUpdateMessage : EntityEventArgs
    {
        public Dictionary<GridId, List<Vector2i>>? NewIndices { get; }

        public TileLookupUpdateMessage(Dictionary<GridId, List<Vector2i>>? indices)
        {
            NewIndices = indices;
        }
    }
}
