using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    public abstract partial class SharedTransformSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
        [Dependency] private readonly MetaDataSystem _metaSys = default!;

        // Needed on release no remove.
        // ReSharper disable once NotAccessedField.Local
        private ISawmill _logger = default!;

        private readonly Queue<MoveEvent> _gridMoves = new();
        private readonly Queue<MoveEvent> _otherMoves = new();

        public override void Initialize()
        {
            base.Initialize();

            _logger = Logger.GetSawmill("transform");
            UpdatesOutsidePrediction = true;

            SubscribeLocalEvent<TileChangedEvent>(MapManagerOnTileChanged);
            SubscribeLocalEvent<TransformComponent, ComponentInit>(OnCompInit);
            SubscribeLocalEvent<TransformComponent, ComponentStartup>(OnCompStartup);
            SubscribeLocalEvent<TransformComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<TransformComponent, ComponentHandleState>(OnHandleState);
            SubscribeLocalEvent<EntParentChangedMessage>(OnParentChange);
        }

        private void OnParentChange(ref EntParentChangedMessage ev)
        {
            // TODO: when PVS errors on live servers get fixed, wrap this whole subscription in an #if DEBUG block to speed up parent changes & entity deletion.
            if (ev.Transform.ParentUid == EntityUid.Invalid)
                return;

            if (LifeStage(ev.Entity) >= EntityLifeStage.Terminating)
                _logger.Error($"Entity {ToPrettyString(ev.Entity)} is getting attached to a new parent while terminating. New parent: {ToPrettyString(ev.Transform.ParentUid)}. Trace: {Environment.StackTrace}");


            if (LifeStage(ev.Transform.ParentUid) >= EntityLifeStage.Terminating)
                _logger.Error($"Entity {ToPrettyString(ev.Entity)} is attaching itself to a terminating entity {ToPrettyString(ev.Transform.ParentUid)}. Trace: {Environment.StackTrace}");
        }

        private void MapManagerOnTileChanged(TileChangedEvent e)
        {
            if(e.NewTile.Tile != Tile.Empty)
                return;

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
            var grid = _mapManager.GetGrid(gridId);
            var gridUid = grid.GridEntityId;
            var mapTransform = Transform(_mapManager.GetMapEntityId(Transform(grid.GridEntityId).MapID));
            var aabb = _entityLookup.GetLocalBounds(tileIndices, grid.TileSize);

            foreach (var entity in _entityLookup.GetEntitiesIntersecting(gridId, tileIndices, LookupFlags.Anchored).ToList())
            {
                // If a tile is being removed due to an explosion or somesuch, some entities are likely being deleted.
                // Avoid unnecessary entity updates.
                if (EntityManager.IsQueuedForDeletion(entity))
                    continue;

                var transform = Transform(entity);
                if (transform.ParentUid == gridUid && aabb.Contains(transform.LocalPosition))
                    transform.AttachParent(mapTransform);
            }
        }

        public void DeferMoveEvent(ref MoveEvent moveEvent)
        {
            if (EntityManager.HasComponent<IMapGridComponent>(moveEvent.Sender))
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

        public EntityCoordinates GetMoverCoordinates(TransformComponent xform, EntityQuery<TransformComponent> xformQuery)
        {
            // GriddUid is only set after init.
            if (xform.LifeStage < ComponentLifeStage.Initialized && xform.GridUid == null)
                SetGridId(xform, xform.FindGridEntityId(xformQuery));

            // Is the entity directly parented to the grid?
            if (xform.GridUid == xform.ParentUid)
                return xform.Coordinates;

            // Is the entity directly parented to the map?
            var mapId = xform.MapUid;
            if (mapId == xform.ParentUid)
                return xform.Coordinates;

            DebugTools.Assert(!_mapManager.IsGrid(xform.Owner) && !_mapManager.IsMap(xform.Owner));

            // Not parented to grid so convert their pos back to the grid.
            var worldPos = GetWorldPosition(xform, xformQuery);

            return xform.GridUid == null
                ? new EntityCoordinates(mapId ?? xform.ParentUid, worldPos)
                : new EntityCoordinates(xform.GridUid.Value, xformQuery.GetComponent(xform.GridUid.Value).InvLocalMatrix.Transform(worldPos));
        }

        /// <summary>
        ///     Variant of <see cref="GetMoverCoordinates"/> that uses a entity coordinates, rather than an entity's transform.
        /// </summary>
        public EntityCoordinates GetMoverCoordinates(EntityCoordinates coordinates, EntityQuery<TransformComponent> xformQuery)
        {
            var parentUid = coordinates.EntityId;
            var parentXform = xformQuery.GetComponent(parentUid);

            // GriddUid is only set after init.
            if (parentXform.LifeStage < ComponentLifeStage.Initialized && parentXform.GridUid == null)
                SetGridId(parentXform, parentXform.FindGridEntityId(xformQuery));

            // Is the entity directly parented to the grid?
            if (parentXform.GridUid == parentUid)
                return coordinates;

            // Is the entity directly parented to the map?
            var mapId = parentXform.MapUid;
            if (mapId == parentUid)
                return coordinates;

            DebugTools.Assert(!_mapManager.IsGrid(parentUid) && !_mapManager.IsMap(parentUid));

            // Not parented to grid so convert their pos back to the grid.
            var worldPos = GetWorldMatrix(parentXform, xformQuery).Transform(coordinates.Position);

            return parentXform.GridUid == null
                ? new EntityCoordinates(mapId ?? parentUid, worldPos)
                : new EntityCoordinates(parentXform.GridUid.Value, xformQuery.GetComponent(parentXform.GridUid.Value).InvLocalMatrix.Transform(worldPos));
        }

        /// <summary>
        ///     Variant of <see cref="GetMoverCoordinates()"/> that also returns the entity's world rotation
        /// </summary>
        public (EntityCoordinates Coords, Angle worldRot) GetMoverCoordinateRotation(TransformComponent xform, EntityQuery<TransformComponent> xformQuery)
        {
            // GriddUid is only set after init.
            if (xform.LifeStage < ComponentLifeStage.Initialized && xform.GridUid == null)
                SetGridId(xform, xform.FindGridEntityId(xformQuery));

            // Is the entity directly parented to the grid?
            if (xform.GridUid == xform.ParentUid)
                return (xform.Coordinates, GetWorldRotation(xform, xformQuery));

            // Is the entity directly parented to the map?
            var mapId = xform.MapUid;
            if (mapId == xform.ParentUid)
                return (xform.Coordinates, GetWorldRotation(xform, xformQuery));

            DebugTools.Assert(!_mapManager.IsGrid(xform.Owner) && !_mapManager.IsMap(xform.Owner));

            var (pos, worldRot) = GetWorldPositionRotation(xform, xformQuery);

            var coords = xform.GridUid == null
                ? new EntityCoordinates(mapId ?? xform.ParentUid, pos)
                : new EntityCoordinates(xform.GridUid.Value, xformQuery.GetComponent(xform.GridUid.Value).InvLocalMatrix.Transform(pos));

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
                return (Vector2i) xform.WorldPosition;

            // We're on a grid, need to convert the coordinates to grid tiles.
            return _mapManager.GetGrid(xform.GridUid.Value).CoordinatesToTile(xform.Coordinates);
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
