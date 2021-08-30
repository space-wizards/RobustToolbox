using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.Client.GameObjects
{
    public sealed class EntityLookup : SharedEntityLookup
    {
        public EntityLookup(IComponentManager compManager, IEntityManager entityManager, IMapManager mapManager) : base(compManager, entityManager, mapManager)
        {
        }

        protected override void UpdatePVSTree(IEntity entity)
        {
            throw new System.NotImplementedException();
        }
    }
}
