using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Handles moving entities between grids as they move around.
    /// </summary>
    internal sealed class SharedGridTraversalSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private Queue<MoveEvent> _queuedEvents = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MoveEvent>(ev => _queuedEvents.Enqueue(ev));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Need to queue because otherwise calling HandleMove during FrameUpdate will lead to prediction isues.
            while (_queuedEvents.TryDequeue(out var moveEvent))
            {
                HandleMove(moveEvent);
            }
        }

        private void HandleMove(MoveEvent moveEvent)
        {
            var entity = moveEvent.Sender;

            if (entity.Deleted ||
                entity.HasComponent<IMapComponent>() ||
                entity.HasComponent<IMapGridComponent>() ||
                entity.IsInContainer())
            {
                return;
            }

            var transform = entity.Transform;

            if (float.IsNaN(moveEvent.NewPosition.X) || float.IsNaN(moveEvent.NewPosition.Y))
            {
                return;
            }

            var mapPos = moveEvent.NewPosition.ToMapPos(EntityManager);

            // Change parent if necessary
            if (_mapManager.TryFindGridAt(transform.MapID, mapPos, out var grid) &&
                grid.GridEntityId.IsValid() &&
                grid.GridEntityId != entity.Uid)
            {
                // Some minor duplication here with AttachParent but only happens when going on/off grid so not a big deal ATM.
                if (grid.Index != transform.GridID)
                {
                    transform.AttachParent(EntityManager.GetEntity(grid.GridEntityId));
                    RaiseLocalEvent(entity.Uid, new ChangedGridMessage(entity, transform.GridID, grid.Index));
                }
            }
            else
            {
                var oldGridId = transform.GridID;

                // Attach them to map / they are on an invalid grid
                if (oldGridId != GridId.Invalid)
                {
                    transform.AttachParent(_mapManager.GetMapEntity(transform.MapID));
                    RaiseLocalEvent(entity.Uid, new ChangedGridMessage(entity, oldGridId, GridId.Invalid));
                }
            }
        }
    }

    public sealed class ChangedGridMessage : EntityEventArgs
    {
        public IEntity Entity;
        public GridId OldGrid;
        public GridId NewGrid;

        public ChangedGridMessage(IEntity entity, GridId oldGrid, GridId newGrid)
        {
            Entity = entity;
            OldGrid = oldGrid;
            NewGrid = newGrid;
        }
    }
}
