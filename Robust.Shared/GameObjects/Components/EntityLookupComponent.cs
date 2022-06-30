using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent]
    public sealed class EntityLookupComponent : Component
    {
        public DynamicTree<EntityUid> Tree = default!;
    }
}
