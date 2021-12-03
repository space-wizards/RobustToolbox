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

        private Stack<MoveEvent> _queuedEvents = new();
        private HashSet<EntityUid> _handledThisTick = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<MoveEvent>((ref MoveEvent ev) => _queuedEvents.Push(ev));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Need to queue because otherwise calling HandleMove during FrameUpdate will lead to prediction issues.
            // TODO: Need to check if that's even still relevant since transform lerping fix?
            ProcessChanges();
        }

        private void ProcessChanges()
        {
            while (_queuedEvents.TryPop(out var moveEvent))
            {
                if (!_handledThisTick.Add(moveEvent.Sender.Uid)) continue;
                HandleMove(ref moveEvent);
            }

            _handledThisTick.Clear();
        }

        private void HandleMove(ref MoveEvent moveEvent)
        {
            var entity = moveEvent.Sender;

            if ((!IoCManager.Resolve<IEntityManager>().EntityExists(entity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity.Uid).EntityLifeStage) >= EntityLifeStage.Deleted ||
                IoCManager.Resolve<IEntityManager>().HasComponent<IMapComponent>(entity.Uid) ||
                IoCManager.Resolve<IEntityManager>().HasComponent<IMapGridComponent>(entity.Uid) ||
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
                EntityManager.TryGetEntity(grid.GridEntityId, out var gridEnt) &&
                grid.GridEntityId != entity.Uid)
            {
                // Some minor duplication here with AttachParent but only happens when going on/off grid so not a big deal ATM.
                if (grid.Index != transform.GridID)
                {
                    transform.AttachParent(gridEnt);
                    RaiseLocalEvent(entity.Uid, new ChangedGridEvent(entity, transform.GridID, grid.Index));
                }
            }
            else
            {
                var oldGridId = transform.GridID;

                // Attach them to map / they are on an invalid grid
                if (oldGridId != GridId.Invalid)
                {
                    transform.AttachParent(_mapManager.GetMapEntity(transform.MapID));
                    RaiseLocalEvent(entity.Uid, new ChangedGridEvent(entity, oldGridId, GridId.Invalid));
                }
            }
        }
    }

    public sealed class ChangedGridEvent : EntityEventArgs
    {
        public IEntity Entity;
        public GridId OldGrid;
        public GridId NewGrid;

        public ChangedGridEvent(IEntity entity, GridId oldGrid, GridId newGrid)
        {
            Entity = entity;
            OldGrid = oldGrid;
            NewGrid = newGrid;
        }
    }
}
