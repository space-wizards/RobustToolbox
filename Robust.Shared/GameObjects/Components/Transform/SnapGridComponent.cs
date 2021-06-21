using System;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Makes it possible to look this entity up with the snap grid.
    /// </summary>
    [Obsolete("Use Transform.Anchored instead of this flag component.")]
    internal class SnapGridComponent : Component
    {
        /// <inheritdoc />
        public sealed override string Name => "SnapGrid";
    }
}
