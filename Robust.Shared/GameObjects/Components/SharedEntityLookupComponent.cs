using Robust.Shared.Physics;

namespace Robust.Shared.GameObjects
{
    public abstract class SharedEntityLookupComponent : Component
    {
        public override string Name => "EntityLookup";

        internal DynamicTree<IEntity> Tree = default!;
    }
}
