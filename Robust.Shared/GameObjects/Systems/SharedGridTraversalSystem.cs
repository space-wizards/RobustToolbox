using System.Collections.Generic;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

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

                if (entity.Deleted || entity.IsInContainer()) continue;

                var transform = entity.Transform;
                // Change parent if necessary
                // Given islands will probably have a bunch of static bodies in them then we'll verify velocities first as it's way cheaper

                // This shoouullddnnn'''tt de-parent anything in a container because none of that should have physics applied to it.
                if (_mapManager.TryFindGridAt(transform.MapID, moveEvent.NewPosition.ToMapPos(EntityManager),
                        out var grid) &&
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

                // Finally we'll handle any GridId changes for parent or children
                var newGridId = grid?.Index ?? GridId.Invalid;
                if (newGridId.Equals(transform.GridID)) continue;

                // If entity is a map / grid ignore or if we have a parent that isn't a map / grid
                if (entity.HasComponent<IMapComponent>() ||
                    entity.HasComponent<IMapGridComponent>() ||
                    entity.Transform.ParentUid.IsValid() && !entity.Transform.Parent!.Owner.HasComponent<IMapComponent>() && !entity.Transform.Parent!.Owner.HasComponent<IMapGridComponent>()) continue;

                transform.GridID = newGridId;
            }
        }

        private void QueueMoveEvent(MoveEvent moveEvent)
        {
            _queuedMoveEvents.Enqueue(moveEvent);
        }
    }
}
