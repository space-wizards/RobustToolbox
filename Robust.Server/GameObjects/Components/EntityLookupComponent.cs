using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedEntityLookupComponent))]
    internal sealed class EntityLookupComponent : SharedEntityLookupComponent
    {
        /// <summary>
        /// A tree to hold entities that have a larger PVS range than their bounds.
        /// </summary>
        public DynamicTree<IEntity> PVSTree = default!;
    }
}
