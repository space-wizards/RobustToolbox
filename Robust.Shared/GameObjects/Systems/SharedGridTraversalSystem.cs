using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Handles moving entities between grids as they move around.
    /// </summary>
    internal sealed class SharedGridTraversalSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        private Stack<MoveEvent> _queuedMoveEvents = new();
        private HashSet<EntityUid> _handledThisTick = new(32);

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MoveEvent>(QueueMoveEvent);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            UnsubscribeLocalEvent<MoveEvent>();
        }

        public override void FrameUpdate(float frameTime)
        {
            base.FrameUpdate(frameTime);
            while (_queuedMoveEvents.TryPop(out var moveEvent))
            {
                var entity = moveEvent.Sender;

                if (_handledThisTick.Contains(entity.Uid)) continue;

                _handledThisTick.Add(entity.Uid);

                if (entity.Deleted ||
                    entity.HasComponent<IMapComponent>() ||
                    entity.HasComponent<IMapGridComponent>() ||
                    entity.IsInContainer())
                {
                    continue;
                }

                var transform = entity.Transform;

                // Change parent if necessary
                if (_mapManager.TryFindGridAt(transform.MapID, moveEvent.NewPosition.ToMapPos(EntityManager), out var grid) &&
                    grid.GridEntityId.IsValid() &&
                    grid.GridEntityId != entity.Uid)
                {
                    // Some minor duplication here with AttachParent but only happens when going on/off grid so not a big deal ATM.
                    if (grid.Index != transform.GridID)
                    {
                        transform.AttachParent(EntityManager.GetEntity(grid.GridEntityId));
                        RaiseLocalEvent(new ChangedGridMessage(entity, transform.GridID, grid.Index));
                    }
                }
                else
                {
                    var oldGridId = transform.GridID;

                    // Attach them to map / they are on an invalid grid
                    if (oldGridId != GridId.Invalid)
                    {
                        transform.AttachParent(_mapManager.GetMapEntity(transform.MapID));
                        RaiseLocalEvent(new ChangedGridMessage(entity, oldGridId, GridId.Invalid));
                    }
                }
            }

            _handledThisTick.Clear();
        }

        private void QueueMoveEvent(MoveEvent moveEvent)
        {
            _queuedMoveEvents.Push(moveEvent);
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
