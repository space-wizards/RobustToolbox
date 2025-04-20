using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    public abstract partial class SharedTransformSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedMapSystem _map = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly INetManager _netMan = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedGridTraversalSystem _traversal = default!;

        private EntityQuery<MapComponent> _mapQuery;
        private EntityQuery<MapGridComponent> _gridQuery;
        private EntityQuery<MetaDataComponent> _metaQuery;
        protected EntityQuery<TransformComponent> XformQuery;

        public delegate void MoveEventHandler(ref MoveEvent ev);

        /// <summary>
        ///     Invoked as an alternative to broadcasting move events, which can be expensive.
        ///     Systems which want to subscribe broadcast to <see cref="MoveEvent"/> (which you probably shouldn't)
        ///     should subscribe to this instead
        /// </summary>
        public event MoveEventHandler? OnGlobalMoveEvent;

        /// <summary>
        ///     Internal move event handlers. This gets invoked before the global & directed move events. This is mainly
        ///     for exception tolerance, we want to ensure that PVS, physics & entity lookups get updated before some
        ///     content code throws an exception.
        /// </summary>
        internal event MoveEventHandler? OnBeforeMoveEvent;

        public override void Initialize()
        {
            base.Initialize();

            UpdatesOutsidePrediction = true;

            _mapQuery = GetEntityQuery<MapComponent>();
            _gridQuery = GetEntityQuery<MapGridComponent>();
            _metaQuery = GetEntityQuery<MetaDataComponent>();
            XformQuery = GetEntityQuery<TransformComponent>();

            SubscribeLocalEvent<TileChangedEvent>(MapManagerOnTileChanged);
            SubscribeLocalEvent<TransformComponent, ComponentInit>(OnCompInit);
            SubscribeLocalEvent<TransformComponent, ComponentStartup>(OnCompStartup);
            SubscribeLocalEvent<TransformComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<TransformComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<TransformComponent, GridAddEvent>(OnGridAdd);
        }

        private void MapManagerOnTileChanged(ref TileChangedEvent e)
        {
            if(e.NewTile.Tile != Tile.Empty)
                return;

            // TODO optimize this for when multiple tiles get empties simultaneously (e.g., explosions).
            DeparentAllEntsOnTile(e.NewTile.GridUid, e.NewTile.GridIndices);
        }

        /// <summary>
        ///     De-parents and unanchors all entities on a grid-tile.
        /// </summary>
        /// <remarks>
        ///     Used when a tile on a grid is removed (becomes space). Only de-parents entities if they are actually
        ///     parented to that grid. No more disemboweling mobs.
        /// </remarks>
        private void DeparentAllEntsOnTile(EntityUid gridId, Vector2i tileIndices)
        {
            if (!TryComp(gridId, out BroadphaseComponent? lookup) || !TryComp<MapGridComponent>(gridId, out var grid))
                return;

            if (!XformQuery.TryGetComponent(gridId, out var gridXform))
                return;

            if (!XformQuery.TryGetComponent(gridXform.MapUid, out var mapTransform))
                return;

            var aabb = _lookup.GetLocalBounds(tileIndices, grid.TileSize);

            foreach (var entity in _lookup.GetLocalEntitiesIntersecting(lookup, aabb, LookupFlags.Uncontained | LookupFlags.Approximate))
            {
                if (!XformQuery.TryGetComponent(entity, out var xform) || xform.ParentUid != gridId)
                    continue;

                if (!aabb.Contains(xform.LocalPosition))
                    continue;

                // If a tile is being removed due to an explosion or somesuch, some entities are likely being deleted.
                // Avoid unnecessary entity updates.
                if (EntityManager.IsQueuedForDeletion(entity))
                    DetachEntity(entity, xform, MetaData(entity), gridXform);
                else
                    SetParent(entity, xform, gridXform.MapUid.Value, mapTransform);
            }
        }

        public EntityCoordinates GetMoverCoordinates(EntityUid uid)
        {
            return GetMoverCoordinates(uid, XformQuery.GetComponent(uid));
        }

        public EntityCoordinates GetMoverCoordinates(EntityUid uid, TransformComponent xform)
        {
            // Nullspace (or map)
            if (!xform.ParentUid.IsValid())
                return xform.Coordinates;

            // GriddUid is only set after init.
            if (!xform._gridInitialized)
                InitializeGridUid(uid, xform);

            // Is the entity directly parented to the grid?
            if (xform.GridUid == xform.ParentUid)
                return xform.Coordinates;

            DebugTools.Assert(!_gridQuery.HasComp(uid) && !_mapQuery.HasComp(uid));

            // Not parented to grid so convert their pos back to the grid.
            var worldPos = GetWorldPosition(xform, XformQuery);

            return xform.GridUid == null
                ? new EntityCoordinates(xform.MapUid ?? xform.ParentUid, worldPos)
                : new EntityCoordinates(xform.GridUid.Value, Vector2.Transform(worldPos, XformQuery.GetComponent(xform.GridUid.Value).InvLocalMatrix));
        }

        public EntityCoordinates GetMoverCoordinates(EntityCoordinates coordinates, EntityQuery<TransformComponent> xformQuery)
        {
            return GetMoverCoordinates(coordinates);
        }

        /// <summary>
        ///     Variant of <see cref="GetMoverCoordinates"/> that uses a entity coordinates, rather than an entity's transform.
        /// </summary>
        public EntityCoordinates GetMoverCoordinates(EntityCoordinates coordinates)
        {
            var parentUid = coordinates.EntityId;

            // Nullspace coordinates?
            if (!parentUid.IsValid())
                return coordinates;

            var parentXform = XformQuery.GetComponent(parentUid);

            // GriddUid is only set after init.
            if (!parentXform._gridInitialized)
                InitializeGridUid(parentUid, parentXform);

            // Is the entity directly parented to the grid?
            if (parentXform.GridUid == parentUid)
                return coordinates;

            // Is the entity directly parented to the map?
            var mapId = parentXform.MapUid;
            if (mapId == parentUid)
                return coordinates;

            DebugTools.Assert(!HasComp<MapGridComponent>(parentUid) && !HasComp<MapComponent>(parentUid));

            // Not parented to grid so convert their pos back to the grid.
            var worldPos = Vector2.Transform(coordinates.Position, GetWorldMatrix(parentXform, XformQuery));

            return parentXform.GridUid == null
                ? new EntityCoordinates(mapId ?? parentUid, worldPos)
                : new EntityCoordinates(parentXform.GridUid.Value, Vector2.Transform(worldPos, XformQuery.GetComponent(parentXform.GridUid.Value).InvLocalMatrix));
        }

        /// <summary>
        ///     Variant of <see cref="GetMoverCoordinates()"/> that also returns the entity's world rotation
        /// </summary>
        public (EntityCoordinates Coords, Angle worldRot) GetMoverCoordinateRotation(EntityUid uid, TransformComponent xform)
        {
            // Nullspace (or map)
            if (!xform.ParentUid.IsValid())
                return (xform.Coordinates, xform.LocalRotation);

            // GriddUid is only set after init.
            if (!xform._gridInitialized)
                InitializeGridUid(uid, xform);

            // Is the entity directly parented to the grid?
            if (xform.GridUid == xform.ParentUid)
                return (xform.Coordinates, GetWorldRotation(xform, XformQuery));

            DebugTools.Assert(!HasComp<MapComponent>(uid) && !HasComp<MapComponent>(uid));

            var (pos, worldRot) = GetWorldPositionRotation(xform, XformQuery);

            var coords = xform.GridUid == null
                ? new EntityCoordinates(xform.MapUid ?? xform.ParentUid, pos)
                : new EntityCoordinates(xform.GridUid.Value, Vector2.Transform(pos, XformQuery.GetComponent(xform.GridUid.Value).InvLocalMatrix));

            return (coords, worldRot);
        }

        /// <summary>
        ///     Helper method that returns the grid or map tile an entity is on.
        /// </summary>
        public Vector2i GetGridOrMapTilePosition(EntityUid uid, TransformComponent? xform = null)
        {
            if(!Resolve(uid, ref xform, false))
                return Vector2i.Zero;

            // Fast path, we're not on a grid.
            if (xform.GridUid == null)
                return GetWorldPosition(xform).Floored();

            // We're on a grid, need to convert the coordinates to grid tiles.
            return _map.CoordinatesToTile(xform.GridUid.Value, Comp<MapGridComponent>(xform.GridUid.Value), xform.Coordinates);
        }

        /// <summary>
        /// Helper method that returns the grid tile an entity is on.
        /// </summary>
        public Vector2i GetGridTilePositionOrDefault(Entity<TransformComponent?> entity, MapGridComponent? grid = null)
        {
            var xform = entity.Comp;
            if(!Resolve(entity.Owner, ref xform) || xform.GridUid == null)
                return Vector2i.Zero;

            if (!Resolve(xform.GridUid.Value, ref grid))
                return Vector2i.Zero;

            return _map.CoordinatesToTile(xform.GridUid.Value, grid, xform.Coordinates);
        }

        /// <summary>
        /// Helper method that returns the grid tile an entity is on.
        /// </summary>
        public bool TryGetGridTilePosition(Entity<TransformComponent?> entity, out Vector2i indices, MapGridComponent? grid = null)
        {
            indices = default;
            var xform = entity.Comp;
            if(!Resolve(entity.Owner, ref xform) || xform.GridUid == null)
                return false;

            if (!Resolve(xform.GridUid.Value, ref grid))
                return false;

            indices = _map.CoordinatesToTile(xform.GridUid.Value, grid, xform.Coordinates);
            return true;
        }

        public void RaiseMoveEvent(
            Entity<TransformComponent, MetaDataComponent> ent,
            EntityUid oldParent,
            Vector2 oldPosition,
            Angle oldRotation,
            EntityUid? oldMap)
        {
            var pos = ent.Comp1._parent == EntityUid.Invalid
                ? default
                : new EntityCoordinates(ent.Comp1._parent, ent.Comp1._localPosition);

            var oldPos = oldParent == EntityUid.Invalid
                ? default
                : new EntityCoordinates(oldParent, oldPosition);

            var ev = new MoveEvent(ent, oldPos, pos, oldRotation, ent.Comp1._localRotation);

            if (oldParent != ent.Comp1._parent)
            {
                _physics.OnParentChange(ent, oldParent, oldMap);
                OnBeforeMoveEvent?.Invoke(ref ev);
                var entParentChangedMessage = new EntParentChangedMessage(ev.Sender, oldParent, oldMap, ev.Component);
                RaiseLocalEvent(ev.Sender, ref entParentChangedMessage, true);
            }
            else
            {
                OnBeforeMoveEvent?.Invoke(ref ev);
            }

            RaiseLocalEvent(ev.Sender, ref ev);
            OnGlobalMoveEvent?.Invoke(ref ev);

            // Finally, handle grid traversal. This is handled separately to avoid out-of-order move events.
            // I.e., if the traversal raises its own move event, this ensures that all the old move event handlers
            // have finished running first. Ideally this shouldn't be required, but this is here just in case
            _traversal.CheckTraverse(ent);
        }
    }

    [ByRefEvent]
    public readonly struct TransformStartupEvent(Entity<TransformComponent> entity)
    {
        public readonly Entity<TransformComponent> Entity = entity;
        public TransformComponent Component => Entity.Comp;
    }

    /// <summary>
    ///     Serialized state of a TransformComponent.
    /// </summary>
    [Serializable, NetSerializable]
    internal readonly record struct TransformComponentState : IComponentState
    {
        /// <summary>
        ///     Current parent entity of this entity.
        /// </summary>
        public readonly NetEntity ParentID;
        // TODO Delta-states
        // If the transform component ever gets delta states, then the client state manager needs to be updated.
        // Currently it explicitly looks for a "TransformComponentState" when determining an entity's parent for the
        // sake of sorting the states that need to be applied base on the transform hierarchy.

        /// <summary>
        ///     Current position offset of the entity.
        /// </summary>
        public readonly Vector2 LocalPosition;

        /// <summary>
        ///     Current rotation offset of the entity.
        /// </summary>
        public readonly Angle Rotation;

        /// <summary>
        /// Is the transform able to be locally rotated?
        /// </summary>
        public readonly bool NoLocalRotation;

        /// <summary>
        /// True if the transform is anchored to a tile.
        /// </summary>
        public readonly bool Anchored;

        /// <summary>
        ///     Constructs a new state snapshot of a TransformComponent.
        /// </summary>
        /// <param name="localPosition">Current position offset of this entity.</param>
        /// <param name="rotation">Current direction offset of this entity.</param>
        /// <param name="parentId">Current parent transform of this entity.</param>
        /// <param name="noLocalRotation"></param>
        public TransformComponentState(Vector2 localPosition, Angle rotation, NetEntity parentId, bool noLocalRotation, bool anchored)
        {
            LocalPosition = localPosition;
            Rotation = rotation;
            ParentID = parentId;
            NoLocalRotation = noLocalRotation;
            Anchored = anchored;
        }
    }
}
