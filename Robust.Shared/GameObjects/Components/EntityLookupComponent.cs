using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent]
    public sealed class EntityLookupComponent : Component
    {
        public override string Name => "EntityLookup";

        internal DynamicTree<IEntity> Tree = default!;
    }
}
