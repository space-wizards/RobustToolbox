using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    [UsedImplicitly]
    internal class SnapGridSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SnapGridComponent, ComponentStartup>(HandleComponentStartup);
            SubscribeLocalEvent<SnapGridComponent, ComponentShutdown>(HandleComponentShutdown);
            SubscribeLocalEvent<SnapGridComponent, MoveEvent>(HandleMoveEvent);
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<SnapGridComponent, ComponentStartup>(HandleComponentStartup);
            UnsubscribeLocalEvent<SnapGridComponent, ComponentShutdown>(HandleComponentShutdown);
            UnsubscribeLocalEvent<SnapGridComponent, MoveEvent>(HandleMoveEvent);
        }

        private void HandleComponentStartup(EntityUid uid, SnapGridComponent component, ComponentStartup args)
        {
            var transform = ComponentManager.GetComponent<ITransformComponent>(uid);
            UpdatePosition(uid, transform, component);
        }

        private void HandleComponentShutdown(EntityUid uid, SnapGridComponent component, ComponentShutdown args)
        {
            if (component.LastGrid == GridId.Invalid)
                return;

            var transform = ComponentManager.GetComponent<ITransformComponent>(uid);

            if (_mapManager.TryGetGrid(component.LastGrid, out var grid))
            {
                var indices = grid.TileIndicesFor(transform.WorldPosition);
                grid.RemoveFromSnapGridCell(indices, uid);
                return;
            }

            component.LastGrid = GridId.Invalid;
        }

        private void HandleMoveEvent(EntityUid uid, SnapGridComponent component, MoveEvent args)
        {
            var transform = ComponentManager.GetComponent<ITransformComponent>(uid);
            UpdatePosition(uid, transform, component);
        }

        private void UpdatePosition(EntityUid euid, ITransformComponent transform, SnapGridComponent snapComp)
        {
            if (snapComp.LastGrid != GridId.Invalid)
            {
                if (!_mapManager.TryGetGrid(snapComp.LastGrid, out var lastGrid))
                {
                    Logger.WarningS("go.comp.snapgrid", "Entity {0} snapgrid didn't find grid {1}. Race condition?", euid, transform.GridID);
                    return;
                }

                lastGrid.RemoveFromSnapGridCell(snapComp.LastTileIndices, euid);
            }

            if (!_mapManager.TryGetGrid(transform.GridID, out var grid))
            {
                // Either a race condition, or we're not on any grids.
                return;
            }

            var oldPos = snapComp.LastTileIndices;
            var oldGrid = snapComp.LastGrid;

            var newPos = grid.TileIndicesFor(transform.MapPosition);
            var newGrid = transform.GridID;

            grid.AddToSnapGridCell(newPos, euid);

            if (oldPos != newPos || oldGrid != newGrid)
            {
                RaiseLocalEvent(euid, new SnapGridPositionChangedEvent(newPos, oldPos, newGrid, oldGrid));
            }

            snapComp.LastTileIndices = newPos;
            snapComp.LastGrid = newGrid;
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
