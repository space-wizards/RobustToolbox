using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Handles moving entities between grids as they move around.
    /// </summary>
    internal sealed class SharedGridTraversalSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedContainerSystem _containers = default!;

        private Stack<MoveEvent> _queuedEvents = new();
        private HashSet<EntityUid> _handledThisTick = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent((ref MoveEvent ev) => _queuedEvents.Push(ev));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            UpdatesOutsidePrediction = true;

            // Need to queue because otherwise calling HandleMove during FrameUpdate will lead to prediction issues.
            // TODO: Need to check if that's even still relevant since transform lerping fix?
            ProcessChanges();
        }

        private void ProcessChanges()
        {
            while (_queuedEvents.TryPop(out var moveEvent))
            {
                if (!_handledThisTick.Add(moveEvent.Sender)) continue;
                HandleMove(ref moveEvent);
            }

            _handledThisTick.Clear();
        }

        private void HandleMove(ref MoveEvent moveEvent)
        {
            var entity = moveEvent.Sender;

            if (EntityManager.Deleted(entity) ||
                EntityManager.HasComponent<IMapComponent>(entity) ||
                EntityManager.HasComponent<IMapGridComponent>(entity))
            {
                return;
            }

            var transform = Transform(entity);

            if (_containers.IsEntityInContainer(entity, transform)) return;

            DebugTools.Assert(!float.IsNaN(moveEvent.NewPosition.X) && !float.IsNaN(moveEvent.NewPosition.Y));

            // Change parent if necessary
            if (_mapManager.TryFindGridAt(moveEvent.NewPosition, out var grid) &&
                EntityManager.EntityExists(grid.GridEntityId) &&
                grid.GridEntityId != entity)
            {
                // Some minor duplication here with AttachParent but only happens when going on/off grid so not a big deal ATM.
                if (grid.Index != transform.GridID)
                {
                    transform.AttachParent(grid.GridEntityId);
                    RaiseLocalEvent(entity, new ChangedGridEvent(entity, transform.GridID, grid.Index));
                }
            }
            else
            {
                var oldGridId = transform.GridID;

                // Attach them to map / they are on an invalid grid
                if (oldGridId != GridId.Invalid)
                {
                    transform.AttachParent(_mapManager.GetMapEntityIdOrThrow(transform.MapID));
                    RaiseLocalEvent(entity, new ChangedGridEvent(entity, oldGridId, GridId.Invalid));
                }
            }
        }
    }

    public sealed class ChangedGridEvent : EntityEventArgs
    {
        public EntityUid Entity;
        public GridId OldGrid;
        public GridId NewGrid;

        public ChangedGridEvent(EntityUid entity, GridId oldGrid, GridId newGrid)
        {
            Entity = entity;
            OldGrid = oldGrid;
            NewGrid = newGrid;
        }
    }
}
