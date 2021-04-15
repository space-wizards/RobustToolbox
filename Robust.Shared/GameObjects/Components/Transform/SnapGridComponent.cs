using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Makes it possible to look this entity up with the snap grid.
    /// </summary>
    public class SnapGridComponent : Component, IComponentDebug
    {
        private const string LogCategory = "go.comp.snapgrid";
        public sealed override string Name => "SnapGrid";

        private bool IsSet;
        [Dependency] private readonly IMapManager _mapManager = default!;

        private GridId _lastGrid;
        public Vector2i Position { get; private set; }

        public static void CompShutdown(SnapGridComponent snapComp)
        {
            if (!snapComp.IsSet)
                return;

            if (snapComp._mapManager.TryGetGrid(snapComp._lastGrid, out var grid))
            {
                grid.RemoveFromSnapGridCell(snapComp.Position, snapComp);
                return;
            }

            snapComp.IsSet = false;
        }

        /// <summary>
        ///     Returns an enumerable over all the entities which are one tile over in a certain direction.
        /// </summary>
        public static IEnumerable<IEntity> GetInDir(SnapGridComponent snapComp, Direction dir)
        {
            if (!snapComp._mapManager.TryGetGrid(snapComp.Owner.Transform.GridID, out var grid))
                return Enumerable.Empty<IEntity>();
            var pos = SnapGridPosAt(snapComp.Position, dir);

            return grid.GetSnapGridCell(pos).Select(s => s.Owner);
        }

        [Pure]
        public static IEnumerable<IEntity> GetOffset(SnapGridComponent snapComp, Vector2i offset)
        {
            if (!snapComp._mapManager.TryGetGrid(snapComp.Owner.Transform.GridID, out var grid))
                return Enumerable.Empty<IEntity>();
            var pos = snapComp.Position + offset;

            return grid.GetSnapGridCell(pos).Select(s => s.Owner);
        }

        public static IEnumerable<IEntity> GetLocal(SnapGridComponent snapComp)
        {
            if (!snapComp._mapManager.TryGetGrid(snapComp.Owner.Transform.GridID, out var grid))
                return Enumerable.Empty<IEntity>();

            return grid.GetSnapGridCell(snapComp.Position).Select(s => s.Owner);
        }

        public string GetDebugString()
        {
            return $"pos: {Position}";
        }

        public static EntityCoordinates DirectionToGrid(SnapGridComponent snapComp, Direction direction)
        {
            if (!snapComp._mapManager.TryGetGrid(snapComp.Owner.Transform.GridID, out var grid))
                return snapComp.Owner.Transform.Coordinates.Offset(direction.ToVec());

            var coords = grid.GridTileToLocal(SnapGridPosAt(snapComp.Position, direction));

            return coords;
        }

        private static Vector2i SnapGridPosAt(Vector2i position, Direction dir, int dist = 1)
        {
            switch (dir)
            {
                case Direction.East:
                    return position + new Vector2i(dist, 0);
                case Direction.SouthEast:
                    return position + new Vector2i(dist, -dist);
                case Direction.South:
                    return position + new Vector2i(0, -dist);
                case Direction.SouthWest:
                    return position + new Vector2i(-dist, -dist);
                case Direction.West:
                    return position + new Vector2i(-dist, 0);
                case Direction.NorthWest:
                    return position + new Vector2i(-dist, dist);
                case Direction.North:
                    return position + new Vector2i(0, dist);
                case Direction.NorthEast:
                    return position + new Vector2i(dist, dist);
                default:
                    throw new NotImplementedException();
            }
        }

        public static IEnumerable<SnapGridComponent> GetCardinalNeighborCells(SnapGridComponent snapComp)
        {
            if (!snapComp._mapManager.TryGetGrid(snapComp.Owner.Transform.GridID, out var grid))
                yield break;

            foreach (var cell in grid.GetSnapGridCell(snapComp.Position))
                yield return cell;
            foreach (var cell in grid.GetSnapGridCell(snapComp.Position + new Vector2i(0, 1)))
                yield return cell;
            foreach (var cell in grid.GetSnapGridCell(snapComp.Position + new Vector2i(0, -1)))
                yield return cell;
            foreach (var cell in grid.GetSnapGridCell(snapComp.Position + new Vector2i(1, 0)))
                yield return cell;
            foreach (var cell in grid.GetSnapGridCell(snapComp.Position + new Vector2i(-1, 0)))
                yield return cell;
        }

        public static IEnumerable<SnapGridComponent> GetCellsInSquareArea(SnapGridComponent snapComp, int n = 1)
        {
            if (!snapComp._mapManager.TryGetGrid(snapComp.Owner.Transform.GridID, out var grid))
                yield break;

            for (var y = -n; y <= n; ++y)
            for (var x = -n; x <= n; ++x)
                foreach (var cell in grid.GetSnapGridCell(snapComp.Position + new Vector2i(x, y)))
                    yield return cell;
        }

        internal static void UpdatePosition(SnapGridComponent snapComp)
        {
            if (snapComp.IsSet)
            {
                if (!snapComp._mapManager.TryGetGrid(snapComp._lastGrid, out var lastGrid))
                {
                    Logger.WarningS(LogCategory, "Entity {0} snapgrid didn't find grid {1}. Race condition?", snapComp.Owner.Uid, snapComp.Owner.Transform.GridID);
                    return;
                }

                lastGrid.RemoveFromSnapGridCell(snapComp.Position, snapComp);
            }

            if (!snapComp._mapManager.TryGetGrid(snapComp.Owner.Transform.GridID, out var grid))
            {
                // Either a race condition, or we're not on any grids.
                return;
            }

            snapComp.IsSet = true;

            var oldPos = snapComp.Position;
            snapComp.Position = grid.SnapGridCellFor(snapComp.Owner.Transform.Coordinates);
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
