using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent]
    public sealed class EntityLookupComponent : Component
    {
        internal DynamicTree<EntityUid> Tree = default!;
    }
}
