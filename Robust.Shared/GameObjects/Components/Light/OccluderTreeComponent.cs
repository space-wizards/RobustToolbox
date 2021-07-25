using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Stores the relevant occluder children of this entity.
    /// </summary>
    public sealed class OccluderTreeComponent : Component
    {
        public override string Name => "OccluderTree";

        internal DynamicTree<OccluderComponent> Tree { get; set; } = default!;
    }
}
