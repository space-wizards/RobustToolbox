using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Makes it possible to look this entity up with the snap grid.
    /// </summary>
    internal class SnapGridComponent : Component
    {
        /// <inheritdoc />
        public sealed override string Name => "SnapGrid";

        /// <summary>
        /// GridId the last time this component was moved.
        /// </summary>
        internal GridId LastGrid = GridId.Invalid;

        /// <summary>
        /// TileIndices the last time this component was moved.
        /// </summary>
        internal Vector2i LastTileIndices;
    }
}
