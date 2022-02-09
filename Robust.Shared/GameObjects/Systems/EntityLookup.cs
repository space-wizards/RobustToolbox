using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    [Flags]
    public enum LookupFlags : byte
    {
        None = 0,
        Approximate = 1 << 0,
        IncludeAnchored = 1 << 1,
        // IncludeGrids = 1 << 2,
    }

    // TODO: Nuke IEntityLookup and just make a system
    public interface IEntityLookup
    {
        // Not an EntitySystem given _entityManager has a dependency on it which means it's just easier to IoC it for tests.

        void Startup();

        void Shutdown();

        bool AnyEntitiesIntersecting(MapId mapId, Box2 box, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesInMap(MapId mapId, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesAt(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesInArc(EntityCoordinates coordinates, float range, Angle direction,
            float arcWidth, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesIntersecting(GridId gridId, IEnumerable<Vector2i> gridIndices);

        IEnumerable<EntityUid> GetEntitiesIntersecting(GridId gridId, Vector2i gridIndices);

        IEnumerable<EntityUid> GetEntitiesIntersecting(TileRef tileRef);

        IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2Rotated worldAABB, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesIntersecting(EntityUid entity, float enlarged = 0f, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesIntersecting(MapCoordinates position, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesIntersecting(EntityCoordinates position, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.IncludeAnchored);

        void FastEntitiesIntersecting(in MapId mapId, ref Box2 worldAABB, EntityUidQueryCallback callback, LookupFlags flags = LookupFlags.IncludeAnchored);

        void FastEntitiesIntersecting(EntityLookupComponent lookup, ref Box2 localAABB, EntityUidQueryCallback callback);

        IEnumerable<EntityUid> GetEntitiesInRange(EntityCoordinates position, float range, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesInRange(EntityUid entity, float range, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesInRange(MapId mapId, Vector2 point, float range, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesInRange(MapId mapId, Box2 box, float range, LookupFlags flags = LookupFlags.IncludeAnchored);

        bool IsIntersecting(EntityUid entityOne, EntityUid entityTwo);

        void RemoveFromEntityTrees(EntityUid entity);

        Box2 GetLocalBounds(TileRef tileRef, ushort tileSize);

        Box2 GetLocalBounds(Vector2i gridIndices, ushort tileSize);

        /// <summary>
        /// Get the AABB of this entity assuming 0,0 position and 0 rotation.
        /// </summary>
        Box2 GetLocalAABB(EntityUid uid, TransformComponent xform);

        Box2Rotated GetWorldBounds(TileRef tileRef, Matrix3? worldMatrix = null, Angle? angle = null);

        Box2 GetWorldAABB(EntityUid uid);

        Box2 GetWorldAABB(EntityUid uid, TransformComponent xform, Vector2 worldPos);

        Box2 GetWorldAABB(EntityUid uid, TransformComponent xform);

        Box2Rotated GetWorldBounds(EntityUid uid, TransformComponent xform);
    }

    [UsedImplicitly]
    public sealed partial class EntityLookup : IEntityLookup, IEntityEventSubscriber
    {
        private readonly IEntityManager _entityManager;
        private readonly IMapManager _mapManager;
        private SharedContainerSystem _container = default!;

        private const int GrowthRate = 256;

        private const float PointEnlargeRange = .00001f / 2;

        /// <summary>
        /// Like RenderTree we need to enlarge our lookup range for EntityLookupComponent as an entity is only ever on
        /// 1 EntityLookupComponent at a time (hence it may overlap without another lookup).
        /// </summary>
        private float _lookupEnlargementRange;

        // TODO: Should combine all of the methods that check for IPhysBody and just use the one GetWorldAabbFromEntity method

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

            _container = EntitySystem.Get<SharedContainerSystem>();
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.LookupEnlargementRange, value => _lookupEnlargementRange = value, true);

            var eventBus = _entityManager.EventBus;

            eventBus.SubscribeEvent<EntParentChangedMessage>(EventSource.Local, this, OnParentChange);
            eventBus.SubscribeEvent<EntityMoveEvent>(EventSource.Local, this, OnEntityMove);
            eventBus.SubscribeEvent<AnchorStateChangedEvent>(EventSource.Local, this, OnAnchored);

            eventBus.SubscribeLocalEvent<EntityLookupComponent, ComponentAdd>(OnLookupAdd);
            eventBus.SubscribeLocalEvent<EntityLookupComponent, ComponentShutdown>(OnLookupShutdown);
            eventBus.SubscribeEvent<GridInitializeEvent>(EventSource.Local, this, OnGridInit);

            _entityManager.EntityDeleted += OnEntityDeleted;
            _entityManager.EntityInitialized += OnEntityInit;
            _mapManager.MapCreated += OnMapCreated;
            Started = true;
        }

        public void Shutdown()
        {
            // If we haven't even started up, there's nothing to clean up then.
            if (!Started)
                return;

            _entityManager.EntityDeleted -= OnEntityDeleted;
            _entityManager.EntityInitialized -= OnEntityInit;
            _mapManager.MapCreated -= OnMapCreated;
            Started = false;
        }

        private void OnAnchored(ref AnchorStateChangedEvent @event)
        {
            // This event needs to be handled immediately as anchoring is handled immediately
            // and any callers may potentially get duplicate entities that just changed state.
            if (@event.Anchored)
            {
                RemoveFromEntityTrees(@event.Entity);
            }
            else if (_entityManager.TryGetComponent(@event.Entity, out MetaDataComponent? meta) && meta.EntityLifeStage < EntityLifeStage.Terminating)
            {
                var xform = _entityManager.GetComponent<TransformComponent>(@event.Entity);
                UpdateEntityTree(@event.Entity, xform);
            }
            // else -> the entity is terminating. We can ignore this un-anchor event, as this entity will be removed by the tree via OnEntityDeleted.
        }

        private void OnLookupShutdown(EntityUid uid, EntityLookupComponent component, ComponentShutdown args)
        {
            component.Tree.Clear();
        }

        private void OnGridInit(GridInitializeEvent ev)
        {
            _entityManager.EnsureComponent<EntityLookupComponent>(ev.EntityUid);
        }

        private void OnLookupAdd(EntityUid uid, EntityLookupComponent component, ComponentAdd args)
        {
            int capacity;

            if (_entityManager.TryGetComponent(uid, out TransformComponent? xform))
            {
                capacity = (int) Math.Min(256, Math.Ceiling(xform.ChildCount / (float) GrowthRate) * GrowthRate);
            }
            else
            {
                capacity = 256;
            }

            component.Tree = new DynamicTree<EntityUid>(
                GetRelativeAABBFromEntity,
                capacity: capacity,
                growthFunc: x => x == GrowthRate ? GrowthRate * 8 : x * 2
            );
        }

        private Box2 GetRelativeAABBFromEntity(in EntityUid entity)
        {
            // TODO: Should feed in AABB to lookup so it's not enlarged unnecessarily

            var tree = GetLookup(entity);
            if (tree == null)
                throw new InvalidOperationException();

            var xform = _entityManager.GetComponent<TransformComponent>(entity);
            var lookupXform = _entityManager.GetComponent<TransformComponent>(tree.Owner);
            var lookupPos =
                new EntityCoordinates(tree.Owner, lookupXform.InvWorldMatrix.Transform(xform.WorldPosition));

            var localAABB = GetLocalAABB(entity, xform);
            var lookupBounds = GetLookupBounds(entity, xform, lookupXform, lookupPos, localAABB);

            return lookupBounds.CalcBoundingBox();
        }

        private void OnEntityDeleted(object? sender, EntityUid uid)
        {
            RemoveFromEntityTrees(uid);
        }

        private void OnEntityInit(object? sender, EntityUid uid)
        {
            var xform = _entityManager.GetComponent<TransformComponent>(uid);
            if (xform.Anchored) return;

            UpdateEntityTree(uid, xform);
        }

        private void OnMapCreated(object? sender, MapEventArgs eventArgs)
        {
            if (eventArgs.Map == MapId.Nullspace) return;

            _mapManager.GetMapEntityId(eventArgs.Map).EnsureComponent<EntityLookupComponent>();
        }

        private void OnParentChange(ref EntParentChangedMessage ev)
        {
            RemoveFromEntityTrees(ev.Entity);
            var xform = _entityManager.GetComponent<TransformComponent>(ev.Entity);

            if (xform.Anchored) return;

            UpdateEntityTree(ev.Entity, xform);
        }

        private void UpdateEntityTree(EntityUid uid, TransformComponent xform)
        {
            var lookup = GetLookup(uid);

            if (lookup == null)
            {
                // TODO: Do we need this?
                RemoveFromEntityTrees(uid);
                return;
            }

            var lookupXform = _entityManager.GetComponent<TransformComponent>(lookup.Owner);
            // TODO: Optimise this 1 call.
            var lookupPos = new EntityCoordinates(lookupXform.Owner,
                lookupXform.InvWorldMatrix.Transform(xform.WorldPosition));

            var localAABB = GetLocalAABB(uid, xform);
            var lookupBounds = GetLookupBounds(uid, xform, lookupXform, lookupPos, localAABB);

            lookup.Tree.Add(uid, lookupBounds.CalcBoundingBox());

            if (!_entityManager.HasComponent<EntityLookupComponent>(uid) && xform.ChildCount > 0)
            {
                DebugTools.Assert(!_entityManager.HasComponent<IMapGridComponent>(uid));

                var children = xform.ChildEnumerator;

                while (children.MoveNext(out var child))
                {
                    var childXform = _entityManager.GetComponent<TransformComponent>(child.Value);

                    UpdateEntityTree(child.Value, childXform);
                }
            }
        }

        #region Spatial Queries

        private LookupsEnumerator GetLookupsIntersecting(MapId mapId, Box2 worldAABB)
        {
            _mapManager.FindGridsIntersectingEnumerator(mapId, worldAABB, out var gridEnumerator, true);

            return new LookupsEnumerator(_entityManager, _mapManager, mapId, gridEnumerator);
        }

        private struct LookupsEnumerator
        {
            private IEntityManager _entityManager;
            private IMapManager _mapManager;

            private MapId _mapId;
            private FindGridsEnumerator _enumerator;

            private bool _final;

            public LookupsEnumerator(IEntityManager entityManager, IMapManager mapManager, MapId mapId, FindGridsEnumerator enumerator)
            {
                _entityManager = entityManager;
                _mapManager = mapManager;

                _mapId = mapId;
                _enumerator = enumerator;
                _final = false;
            }

            public bool MoveNext([NotNullWhen(true)] out EntityLookupComponent? component)
            {
                if (!_enumerator.MoveNext(out var grid))
                {
                    if (_final || _mapId == MapId.Nullspace)
                    {
                        component = null;
                        return false;
                    }

                    _final = true;
                    EntityUid mapUid = _mapManager.GetMapEntityIdOrThrow(_mapId);
                    component = _entityManager.GetComponent<EntityLookupComponent>(mapUid);
                    return true;
                }

                // TODO: Recursive and all that.
                component = _entityManager.GetComponent<EntityLookupComponent>(grid.GridEntityId);
                return true;
            }
        }

        private IEnumerable<EntityUid> GetAnchored(MapId mapId, Box2 worldAABB, LookupFlags flags)
        {
            if ((flags & LookupFlags.IncludeAnchored) == 0x0) yield break;
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
            {
                foreach (var uid in grid.GetAnchoredEntities(worldAABB))
                {
                    if (!_entityManager.EntityExists(uid)) continue;
                    yield return uid;
                }
            }
        }

        private IEnumerable<EntityUid> GetAnchored(MapId mapId, Box2Rotated worldBounds, LookupFlags flags)
        {
            if ((flags & LookupFlags.IncludeAnchored) == 0x0) yield break;
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
            {
                foreach (var uid in grid.GetAnchoredEntities(worldBounds))
                {
                    if (!_entityManager.EntityExists(uid)) continue;
                    yield return uid;
                }
            }
        }

        /// <inheritdoc />
        public bool AnyEntitiesIntersecting(MapId mapId, Box2 box, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var found = false;
            var enumerator = GetLookupsIntersecting(mapId, box);

            while (enumerator.MoveNext(out var lookup))
            {
                var offsetBox = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.TransformBox(box);

                lookup.Tree.QueryAabb(ref found, (ref bool found, in EntityUid ent) =>
                {
                    if (_entityManager.Deleted(ent))
                        return true;

                    found = true;
                    return false;

                }, offsetBox, (flags & LookupFlags.Approximate) != 0x0);
            }

            if (!found)
            {
                foreach (var _ in GetAnchored(mapId, box, flags))
                {
                    return true;
                }
            }

            return found;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void FastEntitiesIntersecting(in MapId mapId, ref Box2 worldAABB, EntityUidQueryCallback callback, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var enumerator = GetLookupsIntersecting(mapId, worldAABB);
            while (enumerator.MoveNext(out var lookup))
            {
                var offsetBox = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.TransformBox(worldAABB);

                lookup.Tree._b2Tree.FastQuery(ref offsetBox, (ref EntityUid data) => callback(data));
            }

            if ((flags & LookupFlags.IncludeAnchored) != 0x0)
            {
                foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
                {
                    foreach (var uid in grid.GetAnchoredEntities(worldAABB))
                    {
                        if (!_entityManager.EntityExists(uid)) continue;
                        callback(uid);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void FastEntitiesIntersecting(EntityLookupComponent lookup, ref Box2 localAABB, EntityUidQueryCallback callback)
        {
            lookup.Tree._b2Tree.FastQuery(ref localAABB, (ref EntityUid data) => callback(data));
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

            var list = new List<EntityUid>();
            var enumerator = GetLookupsIntersecting(mapId, worldAABB);

            while (enumerator.MoveNext(out var lookup))
            {
                var offsetBox = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.TransformBox(worldAABB);

                lookup.Tree.QueryAabb(ref list, (ref List<EntityUid> list, in EntityUid ent) =>
                {
                    if (!_entityManager.Deleted(ent))
                    {
                        list.Add(ent);
                    }
                    return true;
                }, offsetBox, (flags & LookupFlags.Approximate) != 0x0);
            }

            foreach (var ent in GetAnchored(mapId, worldAABB, flags))
            {
                list.Add(ent);
            }

            return list;
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

            var list = new List<EntityUid>();
            var worldAABB = worldBounds.CalcBoundingBox();
            var enumerator = GetLookupsIntersecting(mapId, worldAABB);

            while (enumerator.MoveNext(out var lookup))
            {
                var offsetBox = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.TransformBox(worldBounds);

                lookup.Tree.QueryAabb(ref list, (ref List<EntityUid> list, in EntityUid ent) =>
                {
                    if (!_entityManager.Deleted(ent))
                    {
                        list.Add(ent);
                    }
                    return true;
                }, offsetBox, (flags & LookupFlags.Approximate) != 0x0);
            }

            foreach (var ent in GetAnchored(mapId, worldBounds, flags))
            {
                list.Add(ent);
            }

            return list;
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

            var aabb = new Box2(position, position).Enlarged(PointEnlargeRange);
            var list = new List<EntityUid>();
            var state = (list, position);

            var enumerator = GetLookupsIntersecting(mapId, aabb);

            while (enumerator.MoveNext(out var lookup))
            {
                var localPoint = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.Transform(position);

                lookup.Tree.QueryPoint(ref state, (ref (List<EntityUid> list, Vector2 position) state, in EntityUid ent) =>
                {
                    if (Intersecting(ent, state.position))
                    {
                        state.list.Add(ent);
                    }
                    return true;
                }, localPoint, (flags & LookupFlags.Approximate) != 0x0);
            }

            if ((flags & LookupFlags.IncludeAnchored) != 0x0 &&
                _mapManager.TryFindGridAt(mapId, position, out var grid) &&
                grid.TryGetTileRef(position, out var tile))
            {
                foreach (var uid in grid.GetAnchoredEntities(tile.GridIndices))
                {
                    if (!_entityManager.EntityExists(uid)) continue;
                    state.list.Add(uid);
                }
            }

            return list;
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesIntersecting(MapCoordinates position, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            return GetEntitiesIntersecting(position.MapId, position.Position, flags);
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesIntersecting(EntityCoordinates position, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var mapPos = position.ToMap(_entityManager);
            return GetEntitiesIntersecting(mapPos.MapId, mapPos.Position, flags);
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesIntersecting(EntityUid entity, float enlarged = 0f, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var xform = _entityManager.GetComponent<TransformComponent>(entity);
            var (worldPos, worldRot) = xform.GetWorldPositionRotation();
            var worldAABB = GetWorldAABB(entity, xform, worldPos);

            var enumerator = GetLookupsIntersecting(xform.MapID, worldAABB.Enlarged(_lookupEnlargementRange));
            var list = new List<EntityUid>();

            while (enumerator.MoveNext(out var lookup))
            {
                // To get the tightest bounds possible we'll re-calculate it for each lookup.
                var localBounds = GetLookupBounds(entity, lookup, worldPos, worldRot, enlarged);

                lookup.Tree.QueryAabb(ref list, (ref List<EntityUid> list, in EntityUid ent) =>
                {
                    if (!_entityManager.Deleted(ent))
                    {
                        list.Add(ent);
                    }
                    return true;
                }, localBounds, (flags & LookupFlags.Approximate) != 0x0);
            }

            foreach (var ent in GetAnchored(xform.MapID, worldAABB, flags))
            {
                list.Add(ent);
            }

            return list;
        }

        private Box2 GetLookupBounds(EntityUid uid, EntityLookupComponent lookup, Vector2 worldPos, Angle worldRot, float enlarged)
        {
            var (_, lookupRot, lookupInvWorldMatrix) = _entityManager.GetComponent<TransformComponent>(lookup.Owner).GetWorldPositionRotationInvMatrix();

            var localPos = lookupInvWorldMatrix.Transform(worldPos);
            var localRot = worldRot - lookupRot;

            if (_entityManager.TryGetComponent(uid, out FixturesComponent? manager))
            {
                var transform = new Transform(localPos, localRot);
                Box2? aabb = null;

                foreach (var (_, fixture) in manager.Fixtures)
                {
                    if (!fixture.Hard) continue;
                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                    {
                        aabb = aabb?.Union(fixture.Shape.ComputeAABB(transform, i)) ?? fixture.Shape.ComputeAABB(transform, i);
                    }
                }

                if (aabb != null)
                {
                    return aabb.Value.Enlarged(enlarged);
                }
            }

            // So IsEmpty checks don't get triggered
            return new Box2(localPos - float.Epsilon, localPos + float.Epsilon);
        }

        /// <inheritdoc />
        public bool IsIntersecting(EntityUid entityOne, EntityUid entityTwo)
        {
            var position = _entityManager.GetComponent<TransformComponent>(entityOne).MapPosition.Position;
            return Intersecting(entityTwo, position);
        }

        private bool Intersecting(EntityUid entity, Vector2 mapPosition)
        {
            if (_entityManager.TryGetComponent(entity, out IPhysBody? component))
            {
                if (component.GetWorldAABB().Contains(mapPosition))
                    return true;
            }
            else
            {
                var transform = _entityManager.GetComponent<TransformComponent>(entity);
                var entPos = transform.WorldPosition;
                if (MathHelper.CloseToPercent(entPos.X, mapPosition.X)
                    && MathHelper.CloseToPercent(entPos.Y, mapPosition.Y))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesInRange(EntityCoordinates position, float range, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var mapCoordinates = position.ToMap(_entityManager);
            var mapPosition = mapCoordinates.Position;
            var aabb = new Box2(mapPosition - new Vector2(range, range),
                mapPosition + new Vector2(range, range));
            return GetEntitiesIntersecting(mapCoordinates.MapId, aabb, flags);
            // TODO: Use a circle shape here mate
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesInRange(MapId mapId, Box2 box, float range, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var aabb = box.Enlarged(range);
            return GetEntitiesIntersecting(mapId, aabb, flags);
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesInRange(MapId mapId, Vector2 point, float range, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var aabb = new Box2(point, point).Enlarged(range);
            return GetEntitiesIntersecting(mapId, aabb, flags);
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesInRange(EntityUid entity, float range, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var xform = _entityManager.GetComponent<TransformComponent>(entity);
            var worldAABB = GetWorldAABB(entity, xform);
            return GetEntitiesInRange(xform.MapID, worldAABB, range, flags);
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesInArc(EntityCoordinates coordinates, float range, Angle direction,
            float arcWidth, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var position = coordinates.ToMap(_entityManager).Position;

            foreach (var entity in GetEntitiesInRange(coordinates, range * 2, flags))
            {
                var angle = new Angle(_entityManager.GetComponent<TransformComponent>(entity).WorldPosition - position);
                if (angle.Degrees < direction.Degrees + arcWidth / 2 &&
                    angle.Degrees > direction.Degrees - arcWidth / 2)
                    yield return entity;
            }
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesInMap(MapId mapId, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            DebugTools.Assert((flags & LookupFlags.Approximate) == 0x0);

            foreach (EntityLookupComponent comp in _entityManager.EntityQuery<EntityLookupComponent>(true))
            {
                if (_entityManager.GetComponent<TransformComponent>(comp.Owner).MapID != mapId) continue;

                foreach (var entity in comp.Tree)
                {
                    if (_entityManager.Deleted(entity)) continue;

                    yield return entity;
                }
            }

            if ((flags & LookupFlags.IncludeAnchored) == 0x0) yield break;

            foreach (var grid in _mapManager.GetAllMapGrids(mapId))
            {
                foreach (var tile in grid.GetAllTiles())
                {
                    foreach (var uid in grid.GetAnchoredEntities(tile.GridIndices))
                    {
                        if (!_entityManager.EntityExists(uid)) continue;
                        yield return uid;
                    }
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesAt(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

            var list = new List<EntityUid>();

            var state = (list, position);

            var aabb = new Box2(position, position).Enlarged(PointEnlargeRange);
            var enumerator = GetLookupsIntersecting(mapId, aabb);

            while (enumerator.MoveNext(out var lookup))
            {
                var offsetPos = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.Transform(position);

                lookup.Tree.QueryPoint(ref state, (ref (List<EntityUid> list, Vector2 position) state, in EntityUid ent) =>
                {
                    state.list.Add(ent);
                    return true;
                }, offsetPos, (flags & LookupFlags.Approximate) != 0x0);
            }

            if ((flags & LookupFlags.IncludeAnchored) != 0x0)
            {
                foreach (var grid in _mapManager.FindGridsIntersecting(mapId, aabb))
                {
                    foreach (var uid in grid.GetAnchoredEntities(aabb))
                    {
                        if (!_entityManager.EntityExists(uid)) continue;
                        list.Add(uid);
                    }
                }
            }

            return list;
        }

        #endregion

        #region Entity DynamicTree

        private EntityLookupComponent? GetLookup(EntityUid entity)
        {
            // TODO: This should just be passed in when we cleanup EntityLookup a bit.
            var xforms = _entityManager.GetEntityQuery<TransformComponent>();
            var xform = xforms.GetComponent(entity);

            if (xform.MapID == MapId.Nullspace)
                return null;

            var lookups = _entityManager.GetEntityQuery<EntityLookupComponent>();
            var parent = xform.ParentUid;

            // if it's map return null. Grids should return the map's broadphase.
            if (lookups.HasComponent(entity) &&
                !parent.IsValid())
            {
                return null;
            }

            while (parent.IsValid())
            {
                if (lookups.TryGetComponent(parent, out var comp)) return comp;
                parent = xforms.GetComponent(parent).ParentUid;
            }

            return null;
        }

        private void OnEntityMove(ref EntityMoveEvent ev)
        {
            // Maps and grids get ignored for this; these can be returned via alternative means (grids via gridtrees).
            if (ev.Component.Anchored) return;

            var lookup = _entityManager.GetComponent<EntityLookupComponent>(ev.MoverCoordinates.EntityId);
            var lookupXform = _entityManager.GetComponent<TransformComponent>(lookup.Owner);
            Box2 localAABB;

            // Use the mover's AABB instead.
            if (_container.IsEntityInContainer(ev.Entity, ev.Component))
            {
                localAABB = ev.MoverAABB;
            }
            else
            {
                localAABB = GetLocalAABBNoContainer(ev.Entity, ev.Component);
            }

            var lookupBounds = GetLookupBounds(ev.Entity, ev.Component, lookupXform, ev.MoverCoordinates, localAABB);

            DebugTools.Assert(!float.IsNaN(ev.MoverCoordinates.X) && !float.IsNaN(ev.MoverCoordinates.Y));

            lookup.Tree.Update(ev.Entity, lookupBounds.CalcBoundingBox());
        }

        /// <inheritdoc />
        public void RemoveFromEntityTrees(EntityUid entity)
        {
            // TODO: Need to fix ordering issues and then we can just directly remove it from the tree
            // rather than this O(n) legacy garbage.
            // Also we can't early returns because somehow it gets added to multiple trees!!!
            foreach (var lookup in _entityManager.EntityQuery<EntityLookupComponent>(true))
            {
                lookup.Tree.Remove(entity);
            }
        }

        /// <summary>
        /// Gets the Box2Rotated of this entity relative to its lookup tree.
        /// </summary>
        private Box2Rotated GetLookupBounds(EntityUid uid, TransformComponent xform, TransformComponent lookupXform, EntityCoordinates coordinates, Box2 localAABB)
        {
            DebugTools.Assert(lookupXform.Owner == coordinates.EntityId);

            return new Box2Rotated(localAABB.Translated(coordinates.Position), -lookupXform.WorldRotation,
                coordinates.Position);
        }

        /// <summary>
        /// Assumes the caller has already checked for container. This is useful for recursive moves.
        /// </summary>
        private Box2 GetLocalAABBNoContainer(EntityUid uid, TransformComponent xform)
        {
            DebugTools.Assert(!_container.IsEntityInContainer(uid, xform));
            Box2 localAABB;

            if (_entityManager.TryGetComponent<ILookupWorldBox2Component>(uid, out var worldLookup))
            {
                localAABB = worldLookup.GetLocalAABB();
            }
            else
            {
                localAABB = new Box2();
            }

            return localAABB;
        }

        /// <inheritdoc />
        public Box2 GetLocalAABB(EntityUid uid, TransformComponent xform)
        {
            Box2 localAABB;

            if (_container.TryGetContainingContainer(uid, out var container, xform))
            {
                // Recursively go up and get parent's bounds
                localAABB = GetLocalAABB(container.Owner,
                    _entityManager.GetComponent<TransformComponent>(container.Owner));
            }
            else if (_entityManager.TryGetComponent<ILookupWorldBox2Component>(uid, out var worldLookup))
            {
                localAABB = worldLookup.GetLocalAABB();
            }
            else
            {
                localAABB = new Box2();
            }

            return localAABB;
        }

        public Box2 GetWorldAABB(EntityUid uid)
        {
            var xform = _entityManager.GetComponent<TransformComponent>(uid);
            return GetWorldAABB(uid, xform);
        }

        public Box2 GetWorldAABB(EntityUid uid, TransformComponent xform, Vector2 worldPos)
        {
            var localAABB = GetLocalAABB(uid, xform);
            return localAABB.Translated(worldPos);
        }

        public Box2 GetWorldAABB(EntityUid uid, TransformComponent xform)
        {
            var localAABB = GetLocalAABB(uid, xform);
            return localAABB.Translated(xform.WorldPosition);
        }

        public Box2Rotated GetWorldBounds(EntityUid uid, TransformComponent xform)
        {
            var localAABB = GetLocalAABB(uid, xform);
            var (worldPos, worldRot) = xform.GetWorldPositionRotation();
            return new Box2Rotated(localAABB.Translated(worldPos), worldRot, worldPos);
        }

        #endregion
    }
}
