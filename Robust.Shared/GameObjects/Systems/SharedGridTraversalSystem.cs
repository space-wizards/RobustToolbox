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

        private Queue<MoveEvent> _queuedMoveEvents = new();

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

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            while (_queuedMoveEvents.TryDequeue(out var moveEvent))
            {
                var entity = moveEvent.Sender;

                // TODO: Move the IMapComponent / IMapGridComponent checks up here instead of IsInContainer()
                if (entity.Deleted || entity.IsInContainer()) continue;

                var transform = entity.Transform;
                // Change parent if necessary
                // TODO: AttachParent will also duplicate some of this calculation so remove that.
                if (_mapManager.TryFindGridAt(transform.MapID, transform.WorldPosition, out var grid) &&
                    grid.GridEntityId.IsValid() &&
                    grid.GridEntityId != entity.Uid)
                {
                    // Also this may deparent if 2 entities are parented but not using containers so fix that
                    if (grid.Index != transform.GridID)
                    {
                        transform.AttachParent(EntityManager.GetEntity(grid.GridEntityId));
                    }
                }
                else
                {
                    transform.AttachParent(_mapManager.GetMapEntity(transform.MapID));
                }
            }
        }

        private void QueueMoveEvent(MoveEvent moveEvent)
        {
            _queuedMoveEvents.Enqueue(moveEvent);
        }
    }
}
