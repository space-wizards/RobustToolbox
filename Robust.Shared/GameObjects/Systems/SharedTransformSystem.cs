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
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly INetManager _netMan = default!;

        private EntityQuery<MapGridComponent> _gridQuery;
        private EntityQuery<MetaDataComponent> _metaQuery;
        private EntityQuery<TransformComponent> _xformQuery;

        private readonly Queue<MoveEvent> _gridMoves = new();
        private readonly Queue<MoveEvent> _otherMoves = new();

        public override void Initialize()
        {
            base.Initialize();

            UpdatesOutsidePrediction = true;

            _gridQuery = GetEntityQuery<MapGridComponent>();
            _metaQuery = GetEntityQuery<MetaDataComponent>();
            _xformQuery = GetEntityQuery<TransformComponent>();

            SubscribeLocalEvent<TileChangedEvent>(MapManagerOnTileChanged);
            SubscribeLocalEvent<TransformComponent, ComponentInit>(OnCompInit);
            SubscribeLocalEvent<TransformComponent, ComponentStartup>(OnCompStartup);
            SubscribeLocalEvent<TransformComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<TransformComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<TransformComponent, GridAddEvent>(OnGridAdd);
            SubscribeLocalEvent<EntParentChangedMessage>(OnParentChange);
        }

        private void OnParentChange(ref EntParentChangedMessage ev)
        {
            // TODO: when PVS errors on live servers get fixed, wrap this whole subscription in an #if DEBUG block to speed up parent changes & entity deletion.
            if (ev.Transform.ParentUid == EntityUid.Invalid)
                return;

            if (LifeStage(ev.Entity) >= EntityLifeStage.Terminating)
                Log.Error($"Entity {ToPrettyString(ev.Entity)} is getting attached to a new parent while terminating. New parent: {ToPrettyString(ev.Transform.ParentUid)}. Trace: {Environment.StackTrace}");


            if (LifeStage(ev.Transform.ParentUid) >= EntityLifeStage.Terminating)
                Log.Error($"Entity {ToPrettyString(ev.Entity)} is attaching itself to a terminating entity {ToPrettyString(ev.Transform.ParentUid)}. Trace: {Environment.StackTrace}");
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
            if (!TryComp(gridId, out BroadphaseComponent? lookup) || !_mapManager.TryGetGrid(gridId, out var grid))
                return;

            if (!_xformQuery.TryGetComponent(gridId, out var gridXform))
                return;

            if (!_xformQuery.TryGetComponent(gridXform.MapUid, out var mapTransform))
                return;

            var aabb = _lookup.GetLocalBounds(tileIndices, grid.TileSize);

            foreach (var entity in _lookup.GetEntitiesIntersecting(lookup, aabb, LookupFlags.Uncontained | LookupFlags.Approximate))
            {
                if (!_xformQuery.TryGetComponent(entity, out var xform) || xform.ParentUid != gridId)
                    continue;

                if (!aabb.Contains(xform.LocalPosition))
                    continue;

                // If a tile is being removed due to an explosion or somesuch, some entities are likely being deleted.
                // Avoid unnecessary entity updates.
                if (EntityManager.IsQueuedForDeletion(entity))
                    DetachParentToNull(entity, xform, gridXform);
                else
                    SetParent(entity, xform, gridXform.MapUid.Value, mapTransform);
            }
        }

        public void DeferMoveEvent(ref MoveEvent moveEvent)
        {
            if (EntityManager.HasComponent<MapGridComponent>(moveEvent.Sender))
                _gridMoves.Enqueue(moveEvent);
            else
                _otherMoves.Enqueue(moveEvent);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Process grid moves first.
            Process(_gridMoves);
            Process(_otherMoves);

            void Process(Queue<MoveEvent> queue)
            {
                while (queue.TryDequeue(out var ev))
                {
                    if (EntityManager.Deleted(ev.Sender))
                    {
                        continue;
                    }

                    // Hopefully we can remove this when PVS gets updated to not use NaNs
                    if (!ev.NewPosition.IsValid(EntityManager))
                    {
                        continue;
                    }

                    RaiseLocalEvent(ev.Sender, ref ev, true);
                }
            }
        }

        public EntityCoordinates GetMoverCoordinates(EntityUid uid)
        {
            return GetMoverCoordinates(uid, _xformQuery.GetComponent(uid));
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

            DebugTools.Assert(!_mapManager.IsGrid(uid) && !_mapManager.IsMap(uid));

            // Not parented to grid so convert their pos back to the grid.
            var worldPos = GetWorldPosition(xform, _xformQuery);

            return xform.GridUid == null
                ? new EntityCoordinates(xform.MapUid ?? xform.ParentUid, worldPos)
                : new EntityCoordinates(xform.GridUid.Value, _xformQuery.GetComponent(xform.GridUid.Value).InvLocalMatrix.Transform(worldPos));
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

            var parentXform = _xformQuery.GetComponent(parentUid);

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

            DebugTools.Assert(!_mapManager.IsGrid(parentUid) && !_mapManager.IsMap(parentUid));

            // Not parented to grid so convert their pos back to the grid.
            var worldPos = GetWorldMatrix(parentXform, _xformQuery).Transform(coordinates.Position);

            return parentXform.GridUid == null
                ? new EntityCoordinates(mapId ?? parentUid, worldPos)
                : new EntityCoordinates(parentXform.GridUid.Value, _xformQuery.GetComponent(parentXform.GridUid.Value).InvLocalMatrix.Transform(worldPos));
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
                return (xform.Coordinates, GetWorldRotation(xform, _xformQuery));

            DebugTools.Assert(!_mapManager.IsGrid(uid) && !_mapManager.IsMap(uid));

            var (pos, worldRot) = GetWorldPositionRotation(xform, _xformQuery);

            var coords = xform.GridUid == null
                ? new EntityCoordinates(xform.MapUid ?? xform.ParentUid, pos)
                : new EntityCoordinates(xform.GridUid.Value, _xformQuery.GetComponent(xform.GridUid.Value).InvLocalMatrix.Transform(pos));

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
    }

    [ByRefEvent]
    public readonly struct TransformStartupEvent
    {
        public readonly TransformComponent Component;

        public TransformStartupEvent(TransformComponent component)
        {
            Component = component;
        }
    }

    /// <summary>
    ///     Serialized state of a TransformComponent.
    /// </summary>
    [Serializable, NetSerializable]
    internal sealed class TransformComponentState : ComponentState
    {
        /// <summary>
        ///     Current parent entity of this entity.
        /// </summary>
        public readonly EntityUid ParentID;

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
        public TransformComponentState(Vector2 localPosition, Angle rotation, EntityUid parentId, bool noLocalRotation, bool anchored)
        {
            LocalPosition = localPosition;
            Rotation = rotation;
            ParentID = parentId;
            NoLocalRotation = noLocalRotation;
            Anchored = anchored;
        }
    }
}
