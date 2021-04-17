using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    [UsedImplicitly]
    public class SnapGridSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SnapGridComponent, ComponentInit>(HandleComponentInit);
            SubscribeLocalEvent<SnapGridComponent, ComponentShutdown>(HandleComponentShutdown);
            SubscribeLocalEvent<SnapGridComponent, MoveEvent>(HandleMoveEvent);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<SnapGridComponent, ComponentInit>(HandleComponentInit);
            UnsubscribeLocalEvent<SnapGridComponent, ComponentShutdown>(HandleComponentShutdown);
            UnsubscribeLocalEvent<SnapGridComponent, MoveEvent>(HandleMoveEvent);
        }

        private void HandleComponentInit(EntityUid uid, SnapGridComponent component, ComponentInit args)
        {
            UpdatePosition(component);
        }

        private void HandleComponentShutdown(EntityUid uid, SnapGridComponent component, ComponentShutdown args)
        {
            if (!component.IsSet)
                return;

            var transform = ComponentManager.GetComponent<ITransformComponent>(uid);

            if (_mapManager.TryGetGrid(component._lastGrid, out var grid))
            {
                grid.RemoveFromSnapGridCell(grid.SnapGridCellFor(transform.Coordinates), component);
                return;
            }

            component.IsSet = false;
        }

        private void HandleMoveEvent(EntityUid uid, SnapGridComponent snapGrid, MoveEvent args)
        {
            UpdatePosition(snapGrid);
        }

        //TODO: The event is broken
        private void UpdatePosition(SnapGridComponent snapComp)
        {
            if (snapComp.IsSet)
            {
                if (!_mapManager.TryGetGrid(snapComp._lastGrid, out var lastGrid))
                {
                    Logger.WarningS("go.comp.snapgrid", "Entity {0} snapgrid didn't find grid {1}. Race condition?", snapComp.Owner.Uid, snapComp.Owner.Transform.GridID);
                    return;
                }

                lastGrid.RemoveFromSnapGridCell(snapComp.Position, snapComp);
            }

            if (!_mapManager.TryGetGrid(snapComp.Owner.Transform.GridID, out var grid))
            {
                // Either a race condition, or we're not on any grids.
                return;
            }

            snapComp.IsSet = true;

            var oldPos = snapComp.Position;
            var oldGrid = snapComp._lastGrid;
            snapComp._lastGrid = snapComp.Owner.Transform.GridID;
            grid.AddToSnapGridCell(snapComp.Position, snapComp);

            if (oldPos != snapComp.Position)
            {
                snapComp.Owner.EntityManager.EventBus.RaiseLocalEvent(snapComp.Owner.Uid,
                    new SnapGridPositionChangedEvent(snapComp.Position, oldPos, snapComp._lastGrid, oldGrid));
            }
        }
    }

    public class SnapGridPositionChangedEvent : EntityEventArgs
    {
        public GridId OldGrid { get; }
        public GridId NewGrid { get; }

        public bool SameGrid => OldGrid == NewGrid;

        public Vector2i OldPosition { get; }
        public Vector2i Position { get; }

        public SnapGridPositionChangedEvent(Vector2i position, Vector2i oldPosition, GridId newGrid, GridId oldGrid)
        {
            Position = position;
            OldPosition = oldPosition;

            NewGrid = newGrid;
            OldGrid = oldGrid;
        }
    }
}
