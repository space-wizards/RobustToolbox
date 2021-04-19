using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Makes it possible to look this entity up with the snap grid.
    /// </summary>
    public class SnapGridComponent : Component, IComponentDebug
    {
        public const string LogCategory = "go.comp.snapgrid";
        public sealed override string Name => "SnapGrid";

        private bool IsSet;
        [DataField("offset")]
        private SnapGridOffset _offset = SnapGridOffset.Center;
        [Dependency] private readonly IMapManager _mapManager = default!;

        [Obsolete]
        public event Action? OnPositionChanged;

        private GridId _lastGrid;
        public Vector2i Position { get; private set; }
        public SnapGridOffset Offset => _offset;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            UpdatePosition();
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            base.Shutdown();

            if (IsSet)
            {
                if (_mapManager.TryGetGrid(_lastGrid, out var grid))
                {
                    grid.RemoveFromSnapGridCell(Position, Offset, this);
                    return;
                }

                IsSet = false;
            }
        }

        /// <summary>
        ///     Returns an enumerable over all the entities which are one tile over in a certain direction.
        /// </summary>
        public IEnumerable<IEntity> GetInDir(Direction dir)
        {
            if (!_mapManager.TryGetGrid(Owner.Transform.GridID, out var grid))
                return Enumerable.Empty<IEntity>();
            var pos = SnapGridPosAt(dir);

            return grid.GetSnapGridCell(pos, Offset).Select(s => s.Owner);
        }

        [Pure]
        public IEnumerable<IEntity> GetOffset(Vector2i offset)
        {
            if (!_mapManager.TryGetGrid(Owner.Transform.GridID, out var grid))
                return Enumerable.Empty<IEntity>();
            var pos = Position + offset;

            return grid.GetSnapGridCell(pos, Offset).Select(s => s.Owner);
        }

        public IEnumerable<IEntity> GetLocal()
        {
            if (!_mapManager.TryGetGrid(Owner.Transform.GridID, out var grid))
                return Enumerable.Empty<IEntity>();

            return grid.GetSnapGridCell(Position, Offset).Select(s => s.Owner);
        }


        public string GetDebugString()
        {
            return $"ofs/pos: {Offset}/{Position}";
        }

        public EntityCoordinates DirectionToGrid(Direction direction)
        {
            if (!_mapManager.TryGetGrid(Owner.Transform.GridID, out var grid))
                return Owner.Transform.Coordinates.Offset(direction.ToVec());

            var coords = grid.GridTileToLocal(SnapGridPosAt(direction));

            return coords;
        }

        Vector2i SnapGridPosAt(Direction dir, int dist = 1)
        {
            switch (dir)
            {
                case Direction.East:
                    return Position + new Vector2i(dist, 0);
                case Direction.SouthEast:
                    return Position + new Vector2i(dist, -dist);
                case Direction.South:
                    return Position + new Vector2i(0, -dist);
                case Direction.SouthWest:
                    return Position + new Vector2i(-dist, -dist);
                case Direction.West:
                    return Position + new Vector2i(-dist, 0);
                case Direction.NorthWest:
                    return Position + new Vector2i(-dist, dist);
                case Direction.North:
                    return Position + new Vector2i(0, dist);
                case Direction.NorthEast:
                    return Position + new Vector2i(dist, dist);
                default:
                    throw new NotImplementedException();
            }
        }

        public IEnumerable<SnapGridComponent> GetCardinalNeighborCells()
        {
            if (!_mapManager.TryGetGrid(Owner.Transform.GridID, out var grid))
                yield break;

            foreach (var cell in grid.GetSnapGridCell(Position, Offset))
                yield return cell;
            foreach (var cell in grid.GetSnapGridCell(Position + new Vector2i(0, 1), Offset))
                yield return cell;
            foreach (var cell in grid.GetSnapGridCell(Position + new Vector2i(0, -1), Offset))
                yield return cell;
            foreach (var cell in grid.GetSnapGridCell(Position + new Vector2i(1, 0), Offset))
                yield return cell;
            foreach (var cell in grid.GetSnapGridCell(Position + new Vector2i(-1, 0), Offset))
                yield return cell;
        }

        public IEnumerable<SnapGridComponent> GetCellsInSquareArea(int n = 1)
        {
            if (!_mapManager.TryGetGrid(Owner.Transform.GridID, out var grid))
                yield break;

            for (var y = -n; y <= n; ++y)
            for (var x = -n; x <= n; ++x)
            {
                foreach (var cell in grid.GetSnapGridCell(Position + new Vector2i(x, y), Offset))
                    yield return cell;
            }
        }

        internal void UpdatePosition()
        {
            if (IsSet)
            {
                if (!_mapManager.TryGetGrid(_lastGrid, out var lastGrid))
                {
                    Logger.WarningS(LogCategory, "Entity {0} snapgrid didn't find grid {1}. Race condition?", Owner.Uid, Owner.Transform.GridID);
                    return;
                }

                lastGrid.RemoveFromSnapGridCell(Position, Offset, this);
            }

            if (!_mapManager.TryGetGrid(Owner.Transform.GridID, out var grid))
            {
                // Either a race condition, or we're not on any grids.
                return;
            }

            IsSet = true;

            var oldPos = Position;
            Position = grid.SnapGridCellFor(Owner.Transform.Coordinates, Offset);
            var oldGrid = _lastGrid;
            _lastGrid = Owner.Transform.GridID;
            grid.AddToSnapGridCell(Position, Offset, this);

            if (oldPos != Position)
            {
                OnPositionChanged?.Invoke();
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid,
                    new SnapGridPositionChangedEvent(Position, oldPos, _lastGrid, oldGrid));
            }
        }
    }

    public enum SnapGridOffset: byte
    {
        /// <summary>
        ///     Center snap grid (wires, pipes, ...).
        /// </summary>
        Center,

        /// <summary>
        ///     Edge snap grid (walls, ...).
        /// </summary>
        Edge,
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
