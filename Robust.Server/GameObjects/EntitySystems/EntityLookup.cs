using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.Server.GameObjects
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

        protected override void HandleLookupInit(EntityUid uid, SharedEntityLookupComponent component, ComponentInit args)
        {
            base.HandleLookupInit(uid, component, args);
            var lookup = (EntityLookupComponent) component;
            lookup.PVSTree = new DynamicTree<IEntity>(
                GetBounds,
                capacity: 256,
                growthFunc: x => x == GrowthRate ? GrowthRate * 8 : x + GrowthRate
            );
        }

        /// <summary>
        /// Returns all entities with extended PVS bounds that intersect the supplied AABB.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void FastPVSIntersecting(in MapId mapId, ref Box2 aabb, EntityQueryCallback callback)
        {
            foreach (var lookup in GetLookupsIntersecting(mapId, aabb))
            {
                var serverLookup = (EntityLookupComponent) lookup;
                var offsetBox = aabb.Translated(-lookup.Owner.Transform.WorldPosition);

                serverLookup.PVSTree._b2Tree.FastQuery(ref offsetBox, (ref IEntity data) => callback(data));
            }
        }

        protected override void UpdatePVSTree(SharedEntityLookupComponent component, IEntity entity, Box2 aabb)
        {
            if (!entity.TryGetComponent(out ExtendedPVSRangeComponent? extendedPVS) ||
                !TryGetExtendedBounds(extendedPVS, out var extendedBounds) ||
                extendedBounds.Equals(aabb)) return;

            var lookup = (EntityLookupComponent) component;
            var worldPos = entity.Transform.WorldPosition - lookup.Owner.Transform.WorldPosition;
            lookup.PVSTree.AddOrUpdate(entity, extendedBounds.Value.Translated(worldPos));
        }

        private Box2 GetBounds(in IEntity entity)
        {
            if (!entity.TryGetComponent(out ExtendedPVSRangeComponent? component) ||
                !TryGetExtendedBounds(component, out var extendedAABB))
            {
                return GetRelativeAABBFromEntity(entity);
            }

            var worldPos = entity.Transform.WorldPosition;
            return extendedAABB.Value.Translated(worldPos);
        }

        private bool TryGetExtendedBounds(ExtendedPVSRangeComponent component, [NotNullWhen(true)] out Box2? aabb)
        {
            aabb = null;
            foreach (var (_, bounds) in component.Bounds)
            {
                if (bounds == null) continue;
                aabb = aabb?.Union(bounds.Value) ?? bounds;
            }

            return aabb != null;
        }

        protected override void RemoveFromEntityTrees(IEntity entity)
        {
            base.RemoveFromEntityTrees(entity);
            RemoveFromPVSTree(entity);
        }

        public void RemoveFromPVSTree(IEntity entity)
        {
            // TODO: This is just handling deleted entities. Ideally we'd just get it on component shutdown
            // the main problem is that EntityLookup itself just does this blindly iterating all maps thing

            foreach (var lookup in CompManager.EntityQuery<EntityLookupComponent>(true))
            {
                if (lookup.PVSTree.Remove(entity))
                {
                    return;
                }
            }
        }
    }
}
