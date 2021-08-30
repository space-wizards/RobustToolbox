using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.Client.GameObjects
{
    public sealed class EntityLookup : SharedEntityLookup
    {
        public EntityLookup(IComponentManager compManager, IEntityManager entityManager, IMapManager mapManager) :
            base(compManager, entityManager, mapManager) {}

        protected override void HandleGridInit(GridInitializeEvent ev)
        {
            EntityManager.GetEntity(ev.EntityUid).EnsureComponent<EntityLookupComponent>();
        }

        protected override void HandleMapCreated(object? sender, MapEventArgs eventArgs)
        {
            if (eventArgs.Map == MapId.Nullspace) return;

            MapManager.GetMapEntity(eventArgs.Map).EnsureComponent<EntityLookupComponent>();
        }
    }
}
