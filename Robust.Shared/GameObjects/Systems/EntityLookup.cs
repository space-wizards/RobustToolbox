using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    public delegate void EntityLookupCallback(EntityLookupComponent component);

    [Flags]
    public enum LookupFlags : byte
    {
        None = 0,
        Approximate = 1 << 0,
        IncludeAnchored = 1 << 1,
        // IncludeGrids = 1 << 2,
    }

    public interface IEntityLookup
    {
        // Not an EntitySystem given EntityManager has a dependency on it which means it's just easier to IoC it for tests.

        void Startup();

        void Shutdown();

        void Update();

        void GetLookupsIntersecting(MapId mapId, Box2 worldAABB, EntityLookupCallback callback);

        void GetLookupsIntersecting(MapId mapId, Vector2 worldPos, EntityLookupCallback callback);

        void FastEntitiesIntersecting(in MapId mapId, Box2 position, EntityQueryCallback callback, LookupFlags flags = LookupFlags.IncludeAnchored);
    }

    [UsedImplicitly]
    public class EntityLookup : IEntityLookup, IEntityEventSubscriber
    {
        private readonly IEntityManager _entityManager;
        private readonly IMapManager _mapManager;

        private const int GrowthRate = 256;

        private const float PointEnlargeRange = .00001f / 2;

        // Using stacks so we always use latest data (given we only run it once per entity).
        private readonly Stack<MoveEvent> _moveQueue = new();
        private readonly Stack<RotateEvent> _rotateQueue = new();
        private readonly Queue<EntParentChangedMessage> _parentChangeQueue = new();

        /// <summary>
        /// Like RenderTree we need to enlarge our lookup range for EntityLookupComponent as an entity is only ever on
        /// 1 EntityLookupComponent at a time (hence it may overlap without another lookup).
        /// </summary>
        private float _lookupEnlargementRange;

        /// <summary>
        /// Move and rotate events generate the same update so no point duplicating work in the same tick.
        /// </summary>
        private readonly HashSet<EntityUid> _handledThisTick = new();

        // TODO: Should combine all of the methods that check for IPhysBody and just use the one GetWorldAabbFromEntity method

        // TODO: Combine GridTileLookupSystem and entity anchoring together someday.
        // Queries are a bit of spaghet rn but ideally you'd just have:
        // A) The fast tile-based one
        // B) The physics-only one (given physics needs it to be fast af)
        // C) A generic use one that covers anything not caught in the above.

        public bool Started { get; private set; } = false;

        public EntityLookup(IEntityManager entityManager, IMapManager mapManager)
        {
            _entityManager = entityManager;
            _mapManager = mapManager;
        }

        public void Startup()
        {
            if (Started)
            {
                throw new InvalidOperationException("Startup() called multiple times.");
            }

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.LookupEnlargementRange, value => _lookupEnlargementRange = value, true);

            var eventBus = _entityManager.EventBus;
            eventBus.SubscribeEvent(EventSource.Local, this, (ref MoveEvent ev) => _moveQueue.Push(ev));
            eventBus.SubscribeEvent(EventSource.Local, this, (ref RotateEvent ev) => _rotateQueue.Push(ev));
            eventBus.SubscribeEvent(EventSource.Local, this, (ref EntParentChangedMessage ev) => _parentChangeQueue.Enqueue(ev));
            eventBus.SubscribeEvent<AnchorStateChangedEvent>(EventSource.Local, this, HandleAnchored);

            eventBus.SubscribeLocalEvent<EntityLookupComponent, ComponentInit>(HandleLookupInit);
            eventBus.SubscribeLocalEvent<EntityLookupComponent, ComponentShutdown>(HandleLookupShutdown);
            eventBus.SubscribeEvent<GridInitializeEvent>(EventSource.Local, this, HandleGridInit);

            _entityManager.EntityDeleted += HandleEntityDeleted;
            _entityManager.EntityStarted += HandleEntityStarted;
            _mapManager.MapCreated += HandleMapCreated;
            Started = true;
        }

        public void Shutdown()
        {
            // If we haven't even started up, there's nothing to clean up then.
            if (!Started)
                return;

            _moveQueue.Clear();
            _rotateQueue.Clear();
            _handledThisTick.Clear();
            _parentChangeQueue.Clear();

            _entityManager.EntityDeleted -= HandleEntityDeleted;
            _entityManager.EntityStarted -= HandleEntityStarted;
            _mapManager.MapCreated -= HandleMapCreated;
            Started = false;
        }

        private void HandleAnchored(ref AnchorStateChangedEvent @event)
        {
            // This event needs to be handled immediately as anchoring is handled immediately
            // and any callers may potentially get duplicate entities that just changed state.
            if (@event.Entity.Transform.Anchored)
            {
                RemoveFromEntityTrees(@event.Entity);
            }
            else
            {
                UpdateEntityTree(@event.Entity);
            }
        }

        private void HandleLookupShutdown(EntityUid uid, EntityLookupComponent component, ComponentShutdown args)
        {
            component.Tree.Clear();
        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            _entityManager.GetEntity(ev.EntityUid).EnsureComponent<EntityLookupComponent>();
        }

        private void HandleLookupInit(EntityUid uid, EntityLookupComponent component, ComponentInit args)
        {
            var capacity = (int) Math.Min(256, Math.Ceiling(component.Owner.Transform.ChildCount / (float) GrowthRate) * GrowthRate);

            component.Tree = new DynamicTree<IEntity>(
                GetRelativeAABBFromEntity,
                capacity: capacity,
                growthFunc: x => x == GrowthRate ? GrowthRate * 8 : x + GrowthRate
            );
        }

        private static Box2 GetRelativeAABBFromEntity(in IEntity entity)
        {
            // TODO: Should feed in AABB to lookup so it's not enlarged unnecessarily

            var aabb = GetWorldAABB(entity);
            var tree = GetLookup(entity);

            return tree?.Owner.Transform.InvWorldMatrix.TransformBox(aabb) ?? aabb;
        }

        private void HandleEntityDeleted(object? sender, EntityUid uid)
        {
            RemoveFromEntityTrees(_entityManager.GetEntity(uid));
        }

        private void HandleEntityStarted(object? sender, EntityUid uid)
        {
            var entity = _entityManager.GetEntity(uid);
            if (entity.Transform.Anchored) return;
            UpdateEntityTree(entity);
        }

        private void HandleMapCreated(object? sender, MapEventArgs eventArgs)
        {
            if (eventArgs.Map == MapId.Nullspace) return;

            _mapManager.GetMapEntity(eventArgs.Map).EnsureComponent<EntityLookupComponent>();
        }

        public void Update()
        {
            // Acruid said he'd deal with Update being called around IEntityManager later.

            // Could be more efficient but essentially nuke their old lookup and add to new lookup if applicable.
            while (_parentChangeQueue.TryDequeue(out var mapChangeEvent))
            {
                _handledThisTick.Add(mapChangeEvent.Entity.Uid);
                RemoveFromEntityTrees(mapChangeEvent.Entity);

                if (mapChangeEvent.Entity.Deleted || mapChangeEvent.Entity.Transform.Anchored) continue;
                UpdateEntityTree(mapChangeEvent.Entity, GetWorldAabbFromEntity(mapChangeEvent.Entity));
            }

            while (_moveQueue.TryPop(out var moveEvent))
            {
                if (!_handledThisTick.Add(moveEvent.Sender.Uid) ||
                    moveEvent.Sender.Deleted ||
                    moveEvent.Sender.Transform.Anchored) continue;

                DebugTools.Assert(!moveEvent.Sender.Transform.Anchored);
                UpdateEntityTree(moveEvent.Sender, moveEvent.WorldAABB);
            }

            while (_rotateQueue.TryPop(out var rotateEvent))
            {
                if (!_handledThisTick.Add(rotateEvent.Sender.Uid) ||
                    rotateEvent.Sender.Deleted ||
                    rotateEvent.Sender.Transform.Anchored) continue;

                DebugTools.Assert(!rotateEvent.Sender.Transform.Anchored);
                UpdateEntityTree(rotateEvent.Sender, rotateEvent.WorldAABB);
            }

            _handledThisTick.Clear();
        }

        #region Spatial Queries

        public void GetLookupsIntersecting(MapId mapId, Box2 worldAABB, EntityLookupCallback callback)
        {
            if (mapId == MapId.Nullspace) return;

            // TODO: Recursive and all that.
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB.Enlarged(_lookupEnlargementRange)))
            {
                callback(_entityManager.GetEntity(grid.GridEntityId).GetComponent<EntityLookupComponent>());
            }

            callback(_mapManager.GetMapEntity(mapId).GetComponent<EntityLookupComponent>());
        }

        public void GetLookupsIntersecting(MapId mapId, Vector2 worldPos, EntityLookupCallback callback)
        {
            if (mapId == MapId.Nullspace) return;

            if (_mapManager.TryFindGridAt(mapId, worldPos, out var grid))
            {
                callback(_entityManager.GetEntity(grid.GridEntityId).GetComponent<EntityLookupComponent>());
            }

            // Just to keep it consistent with the AABB version.
            callback(_mapManager.GetMapEntity(mapId).GetComponent<EntityLookupComponent>());
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void FastEntitiesIntersecting(in MapId mapId, Box2 position, EntityQueryCallback callback, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            GetLookupsIntersecting(mapId, position, component =>
            {
                var offsetBox = component.Owner.Transform.InvWorldMatrix.TransformBox(position);

                component.Tree._b2Tree.FastQuery(ref offsetBox, (ref IEntity data) => callback(data));
            });

            if ((flags & LookupFlags.IncludeAnchored) != 0x0)
            {
                foreach (var grid in _mapManager.FindGridsIntersecting(mapId, position))
                {
                    foreach (var uid in grid.GetAnchoredEntities(position))
                    {
                        if (!_entityManager.TryGetEntity(uid, out var ent)) continue;
                        callback(ent);
                    }
                }
            }
        }

        #endregion

        #region Entity DynamicTree

        private static EntityLookupComponent? GetLookup(IEntity entity)
        {
            if (entity.Transform.MapID == MapId.Nullspace)
            {
                return null;
            }

            // if it's map return null. Grids should return the map's broadphase.
            if (entity.HasComponent<EntityLookupComponent>() &&
                entity.Transform.Parent == null)
            {
                return null;
            }

            var parent = entity.Transform.Parent?.Owner;

            while (true)
            {
                if (parent == null) break;

                if (parent.TryGetComponent(out EntityLookupComponent? comp)) return comp;
                parent = parent.Transform.Parent?.Owner;
            }

            return null;
        }

        public virtual bool UpdateEntityTree(IEntity entity, Box2? worldAABB = null)
        {
            // look there's JANK everywhere but I'm just bandaiding it for now for shuttles and we'll fix it later when
            // PVS is more stable and entity anchoring has been battle-tested.
            if (entity.Deleted)
            {
                RemoveFromEntityTrees(entity);
                return true;
            }

            DebugTools.Assert(entity.Initialized);
            DebugTools.Assert(!entity.Transform.Anchored);

            var lookup = GetLookup(entity);

            if (lookup == null)
            {
                RemoveFromEntityTrees(entity);
                return true;
            }

            // Temp PVS guard for when we clear dynamictree for now.
            worldAABB ??= GetWorldAabbFromEntity(entity);
            var center = worldAABB.Value.Center;

            if (float.IsNaN(center.X) || float.IsNaN(center.Y))
            {
                RemoveFromEntityTrees(entity);
                return true;
            }

            var transform = entity.Transform;
            DebugTools.Assert(transform.Initialized);

            var aabb = lookup.Owner.Transform.InvWorldMatrix.TransformBox(worldAABB.Value);

            // for debugging
            var necessary = 0;

            if (lookup.Tree.AddOrUpdate(entity, aabb))
            {
                ++necessary;
            }

            if (!entity.HasComponent<EntityLookupComponent>())
            {
                foreach (var childTx in entity.Transform.ChildEntityUids)
                {
                    if (!_handledThisTick.Add(childTx)) continue;

                    if (UpdateEntityTree(_entityManager.GetEntity(childTx)))
                    {
                        ++necessary;
                    }
                }
            }

            return necessary > 0;
        }

        public void RemoveFromEntityTrees(IEntity entity)
        {
            // TODO: Need to fix ordering issues and then we can just directly remove it from the tree
            // rather than this O(n) legacy garbage.
            foreach (var lookup in _entityManager.EntityQuery<EntityLookupComponent>(true))
            {
                if (lookup.Tree.Remove(entity)) return;
            }
        }

        public Box2 GetWorldAabbFromEntity(in IEntity ent)
        {
            return GetWorldAABB(ent);
        }

        private static Box2 GetWorldAABB(in IEntity ent)
        {
            Vector2 pos;

            if (ent.Deleted)
            {
                pos = ent.Transform.WorldPosition;
                return new Box2(pos, pos);
            }

            if (ent.TryGetContainerMan(out var manager))
            {
                return GetWorldAABB(manager.Owner);
            }

            pos = ent.Transform.WorldPosition;

            return ent.TryGetComponent(out ILookupWorldBox2Component? lookup) ?
                lookup.GetWorldAABB(pos) :
                new Box2(pos, pos);
        }

        #endregion
    }
}
