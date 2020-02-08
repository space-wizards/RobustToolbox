using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects.Components.Transform
{
    /// <summary>
    ///     Makes it possible to look this entity up with the snap grid.
    /// </summary>
    public class SnapGridComponent : Component, IComponentDebug
    {
        public const string LogCategory = "go.comp.snapgrid";
        public sealed override string Name => "SnapGrid";

        private bool IsSet;
        private SnapGridOffset _offset = SnapGridOffset.Center;
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public event Action OnPositionChanged;

        private GridId _lastGrid;
        public MapIndices Position { get; private set; }
        public SnapGridOffset Offset => _offset;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            UpdatePosition();
        }

        /// <inheritdoc />
        protected override void Startup()
        {
            base.Startup();
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

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            if (message is MoveMessage msg && Running)
            {
                UpdatePosition();
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataFieldCached(ref _offset, "offset", SnapGridOffset.Center);
        }

        /// <summary>
        ///     Returns an enumerable over all the entities which are one tile over in a certain direction.
        /// </summary>
        public IEnumerable<IEntity> GetInDir(Direction dir)
        {
            var grid = _mapManager.GetGrid(Owner.Transform.GridID);
            var pos = SnapGridPosAt(dir);

            return grid.GetSnapGridCell(pos, Offset).Select(s => s.Owner);
        }

        public IEnumerable<IEntity> GetLocal()
        {
            var grid = _mapManager.GetGrid(Owner.Transform.GridID);

            return grid.GetSnapGridCell(Position, Offset).Select(s => s.Owner);
        }


        public string GetDebugString()
        {
            return $"ofs/pos: {Offset}/{Position}";
        }

        MapIndices SnapGridPosAt(Direction dir, int dist = 1)
        {
            switch (dir)
            {
                case Direction.East:
                    return Position + new MapIndices(dist, 0);
                case Direction.SouthEast:
                    return Position + new MapIndices(dist, -dist);
                case Direction.South:
                    return Position + new MapIndices(0, -dist);
                case Direction.SouthWest:
                    return Position + new MapIndices(-dist, -dist);
                case Direction.West:
                    return Position + new MapIndices(-dist, 0);
                case Direction.NorthWest:
                    return Position + new MapIndices(-dist, dist);
                case Direction.North:
                    return Position + new MapIndices(0, dist);
                case Direction.NorthEast:
                    return Position + new MapIndices(dist, dist);
                default:
                    throw new NotImplementedException();
            }
        }

        public IEnumerable<SnapGridComponent> GetCardinalNeighborCells()
        {
            var grid = _mapManager.GetGrid(Owner.Transform.GridID);
            foreach (var cell in grid.GetSnapGridCell(Position, Offset))
                yield return cell;
            foreach (var cell in grid.GetSnapGridCell(Position + new MapIndices(0, 1), Offset))
                yield return cell;
            foreach (var cell in grid.GetSnapGridCell(Position + new MapIndices(0, -1), Offset))
                yield return cell;
            foreach (var cell in grid.GetSnapGridCell(Position + new MapIndices(1, 0), Offset))
                yield return cell;
            foreach (var cell in grid.GetSnapGridCell(Position + new MapIndices(-1, 0), Offset))
                yield return cell;
        }

        public IEnumerable<SnapGridComponent> GetCellsInSquareArea(int n = 1)
        {
            var grid = _mapManager.GetGrid(Owner.Transform.GridID);
            for (var y = -n; y <= n; ++y)
            for (var x = -n; x <= n; ++x)
            {
                foreach (var cell in grid.GetSnapGridCell(Position + new MapIndices(x, y), Offset))
                    yield return cell;
            }
        }

        private void UpdatePosition()
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
                Logger.WarningS(LogCategory, "Entity {0} snapgrid didn't find grid {1}. Race condition?", Owner.Uid, Owner.Transform.GridID);
                return;
            }

            IsSet = true;

            var oldPos = Position;
            Position = grid.SnapGridCellFor(Owner.Transform.GridPosition, Offset);
            _lastGrid = Owner.Transform.GridID;
            grid.AddToSnapGridCell(Position, Offset, this);

            if (oldPos != Position)
            {
                OnPositionChanged?.Invoke();
            }
        }
    }

    public enum SnapGridOffset
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
}
