using System;
using System.Collections.Generic;
using System.Linq;
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
    public interface IEntityLookup
    {
        // Not an EntitySystem given EntityManager has a dependency on it which means it's just easier to IoC it for tests.

        void Startup();

        void Shutdown();

        void Update();
        bool AnyEntitiesIntersecting(MapId mapId, Box2 box, LookupFlags flags = LookupFlags.None);

        IEnumerable<IEntity> GetEntitiesInMap(MapId mapId);

        IEnumerable<IEntity> GetEntitiesAt(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.Contained);

        IEnumerable<IEntity> GetEntitiesInArc(EntityCoordinates coordinates, float range, Angle direction,
            float arcWidth, LookupFlags flags = LookupFlags.Contained);

        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position, LookupFlags flags = LookupFlags.Contained);

        IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity, LookupFlags flags = LookupFlags.Contained);

        IEnumerable<IEntity> GetEntitiesIntersecting(MapCoordinates position, LookupFlags flags = LookupFlags.Contained);

        IEnumerable<IEntity> GetEntitiesIntersecting(EntityCoordinates position, LookupFlags flags = LookupFlags.Contained);

        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.Contained);

        void FastEntitiesIntersecting(in MapId mapId, ref Box2 position, EntityQueryCallback callback, LookupFlags flags = LookupFlags.All);

        IEnumerable<IEntity> GetEntitiesInRange(EntityCoordinates position, float range, LookupFlags flags = LookupFlags.Contained);

        IEnumerable<IEntity> GetEntitiesInRange(IEntity entity, float range, LookupFlags flags = LookupFlags.Contained);

        IEnumerable<IEntity> GetEntitiesInRange(MapId mapId, Vector2 point, float range, LookupFlags flags = LookupFlags.Contained);

        bool IsIntersecting(IEntity entityOne, IEntity entityTwo);

        /// <summary>
        /// Updates the lookup for this entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="worldAABB">Pass in to avoid it being re-calculated</param>
        /// <returns></returns>
        bool UpdateEntityTree(IEntity entity, Box2? worldAABB = null);

        void RemoveFromEntityTrees(IEntity entity);

        Box2 GetWorldAabbFromEntity(in IEntity ent);
    }

    [UsedImplicitly]
    public class EntityLookup : IEntityLookup, IEntityEventSubscriber
    {
        private readonly IComponentManager _compManager;
        private readonly IEntityManager _entityManager;
        private readonly IMapManager _mapManager;

        private const int GrowthRate = 256;

        private const float PointEnlargeRange = .00001f / 2;

        // Using stacks so we always use latest data (given we only run it once per entity).
        private readonly Stack<MoveEvent> _moveQueue = new();
        private readonly Stack<RotateEvent> _rotateQueue = new();
        private readonly Stack<ContainerModifiedMessage> _containerQueue = new();
        private readonly Queue<EntParentChangedMessage> _parentChangeQueue = new();
        private readonly Queue<ContainerModifiedMessage> _containerQueue = new();

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

        public EntityLookup(IComponentManager compManager, IEntityManager entityManager, IMapManager mapManager)
        {
            _compManager = compManager;
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
            eventBus.SubscribeEvent<MoveEvent>(EventSource.Local, this, ev => _moveQueue.Push(ev));
            eventBus.SubscribeEvent<RotateEvent>(EventSource.Local, this, ev => _rotateQueue.Push(ev));
            eventBus.SubscribeEvent<EntParentChangedMessage>(EventSource.Local, this, ev => _parentChangeQueue.Enqueue(ev));
            eventBus.SubscribeEvent<EntInsertedIntoContainerMessage>(EventSource.Local, this, ev => _containerQueue.Enqueue(ev));
            eventBus.SubscribeEvent<EntRemovedFromContainerMessage>(EventSource.Local, this, ev => _containerQueue.Enqueue(ev));

            eventBus.SubscribeEvent<EntInsertedIntoContainerMessage>(EventSource.Local, this, ev => _containerQueue.Push(ev));
            eventBus.SubscribeEvent<EntRemovedFromContainerMessage>(EventSource.Local, this, ev => _containerQueue.Push(ev));

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
            _containerQueue.Clear();

            _entityManager.EntityDeleted -= HandleEntityDeleted;
            _entityManager.EntityStarted -= HandleEntityStarted;
            _mapManager.MapCreated -= HandleMapCreated;
            Started = false;
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
            var aabb = GetWorldAABB(entity);
            var tree = GetLookup(entity);

            return aabb.Translated(-tree?.Owner.Transform.WorldPosition ?? Vector2.Zero);
        }

        private void HandleEntityDeleted(object? sender, EntityUid uid)
        {
            RemoveFromEntityTrees(_entityManager.GetEntity(uid));
        }

        private void HandleEntityStarted(object? sender, EntityUid uid)
        {
            UpdateEntityTree(_entityManager.GetEntity(uid));
        }

        private void HandleMapCreated(object? sender, MapEventArgs eventArgs)
        {
            if (eventArgs.Map == MapId.Nullspace) return;

            _mapManager.GetMapEntity(eventArgs.Map).EnsureComponent<EntityLookupComponent>();
        }

        public void Update()
        {
            // Acruid said he'd deal with Update being called around IEntityManager later.

            // TODO: We should order all events and then run through them rather than this kinda spaghet. Thanks sloth.

            // Could be more efficient but essentially nuke their old lookup and add to new lookup if applicable.
            while (_parentChangeQueue.TryDequeue(out var mapChangeEvent))
            {
                _handledThisTick.Add(mapChangeEvent.Entity.Uid);
                RemoveFromEntityTrees(mapChangeEvent.Entity);

                if (mapChangeEvent.Entity.Deleted ||
                    mapChangeEvent.Entity.IsInContainer()) continue;

                UpdateEntityTree(mapChangeEvent.Entity, GetWorldAabbFromEntity(mapChangeEvent.Entity));
            }

            // Because a parent change would conclusively update our position we'll skip any potential other queued events as an optimisation.

            // As it's a Stack we'll always have the latest event so can just skip the rest.
            // This /should/ be handled by the parent change but juusssttt in case.
            // In the future when we have more robust tests and EntityLookup is cleaned up we can probably remove this.
            // EntityLookup isn't even that expensive ATM so not a huge prio.
            while (_containerQueue.TryPop(out var containerEvent))
            {
                if (containerEvent.Entity.Deleted || !_handledThisTick.Add(containerEvent.Entity.Uid)) continue;

                switch (containerEvent)
                {
                    case EntInsertedIntoContainerMessage:
                        RemoveFromEntityTrees(containerEvent.Entity);
                        break;
                    case EntRemovedFromContainerMessage:
                        UpdateEntityTree(containerEvent.Entity);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            while (_moveQueue.TryPop(out var moveEvent))
            {
                if (moveEvent.Sender.Deleted || !_handledThisTick.Add(moveEvent.Sender.Uid)) continue;

                UpdateEntityTree(moveEvent.Sender, moveEvent.WorldAABB);
            }

            while (_rotateQueue.TryPop(out var rotateEvent))
            {
                if (rotateEvent.Sender.Deleted || !_handledThisTick.Add(rotateEvent.Sender.Uid)) continue;

                UpdateEntityTree(rotateEvent.Sender, rotateEvent.WorldAABB);
            }

            _handledThisTick.Clear();
        }

        #region Spatial Queries

        private IEnumerable<EntityLookupComponent> GetLookupsIntersecting(MapId mapId, Box2 worldAABB)
        {
            if (mapId == MapId.Nullspace) yield break;

            // TODO: Recursive and all that.
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB.Enlarged(_lookupEnlargementRange)))
            {
                yield return _entityManager.GetEntity(grid.GridEntityId).GetComponent<EntityLookupComponent>();
            }

            yield return _mapManager.GetMapEntity(mapId).GetComponent<EntityLookupComponent>();
        }

        /// <summary>
        /// Add any contained entities if the flag is set.
        /// </summary>
        private void IncludeContained(LookupFlags flags, IEntity entity, List<IEntity> entities)
        {
            if ((flags & LookupFlags.Contained) == 0x0 ||
                !entity.TryGetComponent(out ContainerManagerComponent? manager)) return;

            foreach (var container in manager.GetAllContainers())
            {
                foreach (var child in container.ContainedEntities)
                {
                    if (child.Deleted) continue;
                    entities.Add(child);
                }
            }
        }

        /// <inheritdoc />
        public bool AnyEntitiesIntersecting(MapId mapId, Box2 box, LookupFlags flags = LookupFlags.None)
        {
            DebugTools.Assert((flags & LookupFlags.Contained) == 0);
            var found = false;

            foreach (var lookup in GetLookupsIntersecting(mapId, box))
            {
                var offsetBox = box.Translated(-lookup.Owner.Transform.WorldPosition);

                lookup.Tree.QueryAabb(ref found, (ref bool found, in IEntity ent) =>
                {
                    if (ent.Deleted) return true;
                    found = true;
                    return false;

                }, offsetBox, (flags & LookupFlags.Approximate) != 0);
            }

            return found;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void FastEntitiesIntersecting(in MapId mapId, ref Box2 position, EntityQueryCallback callback, LookupFlags flags = LookupFlags.All)
        {
            foreach (var lookup in GetLookupsIntersecting(mapId, position))
            {
                var offsetBox = position.Translated(-lookup.Owner.Transform.WorldPosition);

                lookup.Tree._b2Tree.FastQuery(ref offsetBox, (ref IEntity data) =>
                {
                    callback(data);

                    if ((flags & LookupFlags.Contained) != 0 && data.TryGetComponent(out ContainerManagerComponent? managerComponent))
                    {
                        foreach (var container in managerComponent.GetAllContainers())
                        {
                            foreach (var child in container.ContainedEntities)
                            {
                                callback(child);
                            }
                        }
                    }
                });
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position, LookupFlags flags = LookupFlags.Contained)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<IEntity>();

            var list = new List<IEntity>();

            foreach (var lookup in GetLookupsIntersecting(mapId, position))
            {
                var offsetBox = position.Translated(-lookup.Owner.Transform.WorldPosition);

                lookup.Tree.QueryAabb(ref list, (ref List<IEntity> list, in IEntity ent) =>
                {
                    if (ent.Deleted) return true;
                    list.Add(ent);
                    IncludeContained(flags, ent, list);
                    return true;
                }, offsetBox, (flags & LookupFlags.Approximate) != 0);
            }

            return list;
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.Contained)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<IEntity>();

            var list = new List<IEntity>();
            var state = (list, position);

            foreach (var lookup in GetLookupsIntersecting(mapId, new Box2(position, position)))
            {
                var offsetBox = lookup.Owner.Transform.InvWorldMatrix.Transform(position);

                lookup.Tree.QueryPoint(ref state, (ref (List<IEntity> list, Vector2 position) state, in IEntity ent) =>
                {
                    if (ent.Deleted) return true;
                    state.list.Add(ent);
                    IncludeContained(flags, ent, list);
                    return true;
                }, offsetBox, (flags & LookupFlags.Approximate) != 0);
            }

            return list;
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapCoordinates position, LookupFlags flags = LookupFlags.Contained)
        {
            return GetEntitiesIntersecting(position.MapId, position.Position, flags);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(EntityCoordinates position, LookupFlags flags = LookupFlags.Contained)
        {
            var mapPos = position.ToMap(_entityManager);
            return GetEntitiesIntersecting(mapPos.MapId, mapPos.Position, flags);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity, LookupFlags flags = LookupFlags.Contained)
        {
            return GetEntitiesIntersecting(entity.Transform.Coordinates, flags);
        }

        /// <inheritdoc />
        public bool IsIntersecting(IEntity entityOne, IEntity entityTwo)
        {
            var position = entityOne.Transform.MapPosition.Position;
            return Intersecting(entityTwo, position);
        }

        private bool Intersecting(IEntity entity, Vector2 mapPosition)
        {
            if (entity.TryGetComponent(out IPhysBody? component))
            {
                if (component.GetWorldAABB().Contains(mapPosition))
                    return true;
            }
            else
            {
                var transform = entity.Transform;
                var entPos = transform.WorldPosition;
                if (MathHelper.CloseTo(entPos.X, mapPosition.X)
                    && MathHelper.CloseTo(entPos.Y, mapPosition.Y))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(EntityCoordinates position, float range, LookupFlags flags = LookupFlags.Contained)
        {
            var mapCoordinates = position.ToMap(_entityManager);
            var mapPosition = mapCoordinates.Position;

            // TODO: This needs to be a circle and we need to add support for dis via ICollisionManager
            var aabb = new Box2(mapPosition, mapPosition).Enlarged(range);
            return GetEntitiesIntersecting(mapCoordinates.MapId, aabb, flags);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(MapId mapId, Vector2 point, float range, LookupFlags flags = LookupFlags.Contained)
        {
            // TODO: This needs to use a circle m8, same with the other range ones.
            var aabb = new Box2(point, point).Enlarged(range);
            return GetEntitiesIntersecting(mapId, aabb, flags);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(IEntity entity, float range, LookupFlags flags = LookupFlags.Contained)
        {
            return GetEntitiesInRange(entity.Transform.Coordinates, range, flags);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInArc(EntityCoordinates coordinates, float range, Angle direction,
            float arcWidth, LookupFlags flags = LookupFlags.Contained)
        {
            var position = coordinates.ToMap(_entityManager).Position;

            foreach (var entity in GetEntitiesInRange(coordinates, range * 2, flags))
            {
                var angle = new Angle(entity.Transform.WorldPosition - position);
                if (angle.Degrees < direction.Degrees + arcWidth / 2 &&
                    angle.Degrees > direction.Degrees - arcWidth / 2)
                    yield return entity;
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInMap(MapId mapId)
        {
            foreach (EntityLookupComponent comp in _compManager.EntityQuery<EntityLookupComponent>(true))
            {
                foreach (var entity in comp.Tree)
                {
                    if (entity.Deleted) continue;

                    if (entity.TryGetComponent(out ContainerManagerComponent? containerManager))
                    {
                        foreach (var container in containerManager.GetAllContainers())
                        {
                            foreach (var child in container.ContainedEntities)
                            {
                                if (child.Deleted) continue;
                                yield return child;
                            }
                        }
                    }

                    yield return entity;

                    if (!entity.TryGetComponent(out ContainerManagerComponent? manager)) continue;

                    foreach (var container in manager.GetAllContainers())
                    {
                        foreach (var child in container.ContainedEntities)
                        {
                            if (!child.Deleted) continue;
                            yield return child;
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesAt(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.Contained)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<IEntity>();

            var list = new List<IEntity>();

            var state = (list, position);

            var aabb = new Box2(position, position).Enlarged(PointEnlargeRange);

            foreach (var lookup in GetLookupsIntersecting(mapId, aabb))
            {
                var offsetPos = lookup.Owner.Transform.InvWorldMatrix.Transform(position);

                lookup.Tree.QueryPoint(ref state, (ref (List<IEntity> list, Vector2 position) state, in IEntity ent) =>
                {
                    if (ent.Deleted) return true;
                    state.list.Add(ent);
                    IncludeContained(flags, ent, list);
                    return true;
                }, offsetPos, (flags & LookupFlags.Approximate) != 0x0);
            }

            return list;
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

        /// <inheritdoc />
        public bool UpdateEntityTree(IEntity entity, Box2? worldAABB = null)
        {
            // look there's JANK everywhere but I'm just bandaiding it for now for shuttles and we'll fix it later when
            // PVS is more stable and entity anchoring has been battle-tested.
            if (entity.Deleted || entity.IsInContainer())
            {
                RemoveFromEntityTrees(entity);
                return true;
            }

            DebugTools.Assert(entity.Initialized);

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

            var aabb = worldAABB.Value.Translated(-lookup.Owner.Transform.WorldPosition);

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

        /// <inheritdoc />
        public void RemoveFromEntityTrees(IEntity entity)
        {
            foreach (var lookup in _compManager.EntityQuery<EntityLookupComponent>(true))
            {
                lookup.Tree.Remove(entity);
            }
        }

        public Box2 GetWorldAabbFromEntity(in IEntity ent)
        {
            return GetWorldAABB(ent);
        }

        private static Box2 GetWorldAABB(in IEntity ent)
        {
            if (ent.TryGetContainerMan(out var containerManager))
            {
                return GetWorldAABB(containerManager.Owner);
            }

            var pos = ent.Transform.WorldPosition;

            if (ent.Deleted || !ent.TryGetComponent(out ILookupWorldBox2Component? lookup))
                return new Box2(pos, pos);

            return lookup.GetWorldAABB(pos);
        }

        #endregion
    }

    [Flags]
    public enum LookupFlags : byte
    {
        None = 0,

        /// <summary>
        /// Whether entities inside a container should be included.
        /// </summary>
        Contained = 1 << 0,

        /// <summary>
        /// Should we do an approximate bounds check.
        /// </summary>
        Approximate = 1 << 1,
    }
}
