using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    public abstract partial class SharedTransformSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

        // Needed on release no remove.
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
        }

        private void MapManagerOnTileChanged(TileChangedEvent e)
        {
            if(e.NewTile.Tile != Tile.Empty)
                return;

            DeparentAllEntsOnTile(e.NewTile.GridIndex, e.NewTile.GridIndices);
        }

        /// <summary>
        ///     De-parents and unanchors all entities on a grid-tile.
        /// </summary>
        /// <remarks>
        ///     Used when a tile on a grid is removed (becomes space). Only de-parents entities if they are actually
        ///     parented to that grid. No more disemboweling mobs.
        /// </remarks>
        private void DeparentAllEntsOnTile(GridId gridId, Vector2i tileIndices)
        {
            var grid = _mapManager.GetGrid(gridId);
            var gridUid = grid.GridEntityId;
            var mapTransform = Transform(_mapManager.GetMapEntityId(grid.ParentMapId));
            var aabb = _entityLookup.GetLocalBounds(tileIndices, grid.TileSize);

            foreach (var entity in _entityLookup.GetEntitiesIntersecting(gridId, tileIndices).ToList())
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

                    RaiseLocalEvent(ev.Sender, ref ev);
                }
            }
        }

        public EntityCoordinates GetMoverCoordinates(TransformComponent xform)
        {
            // If they're parented directly to the map or grid then just return the coordinates.
            if (!_mapManager.TryGetGrid(xform.GridID, out var grid))
            {
                var mapUid = _mapManager.GetMapEntityId(xform.MapID);
                var coordinates = xform.Coordinates;

                // Parented directly to the map.
                if (xform.ParentUid == mapUid)
                    return coordinates;

                return new EntityCoordinates(mapUid, coordinates.ToMapPos(EntityManager));
            }

            // Parented directly to the grid
            if (grid.GridEntityId == xform.ParentUid)
                return xform.Coordinates;

            // Parented to grid so convert their pos back to the grid.
            var gridPos = Transform(grid.GridEntityId).InvWorldMatrix.Transform(xform.WorldPosition);
            return new EntityCoordinates(grid.GridEntityId, gridPos);
        }

        public EntityCoordinates GetMoverCoordinates(EntityCoordinates coordinates, EntityQuery<TransformComponent> xformQuery)
        {
            // GridID isn't ready during EntityInit so YAY
            IMapGrid? grid = null;
            var ent = coordinates.EntityId;

            while (ent.IsValid())
            {
                if (_mapManager.TryGetGrid(ent, out grid))
                    break;

                ent = xformQuery.GetComponent(ent).ParentUid;
            }

            // If they're parented directly to the map or grid then just return the coordinates.
            if (grid == null)
            {
                var mapPos = coordinates.ToMap(EntityManager);
                var mapUid = _mapManager.GetMapEntityId(mapPos.MapId);

                // Parented directly to the map.
                if (coordinates.EntityId == mapUid)
                    return coordinates;

                return new EntityCoordinates(mapUid, mapPos.Position);
            }

            // Parented directly to the grid
            if (grid.GridEntityId == coordinates.EntityId)
                return coordinates;

            // Parented to grid so convert their pos back to the grid.
            var gridPos = Transform(grid.GridEntityId).InvWorldMatrix.Transform(coordinates.ToMapPos(EntityManager));
            return new EntityCoordinates(grid.GridEntityId, gridPos);
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
