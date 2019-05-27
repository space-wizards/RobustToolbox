using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
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

        public MapIndices Position { get; private set; }
        public SnapGridOffset Offset => _offset;


        public override void Startup()
        {
            base.Startup();

            Owner.Transform.OnMove += OnTransformMove;
            UpdatePosition();
        }

        public override void Shutdown()
        {
            base.Shutdown();

            Owner.Transform.OnMove -= OnTransformMove;
            if (IsSet)
            {
                if (!_mapManager.TryGetGrid(Owner.Transform.GridID, out var grid))
                {
                    Logger.WarningS(LogCategory, "Entity {0} snapgrid didn't find grid {1}. Race condition?", Owner.Uid, Owner.Transform.GridID);
                    return;
                }

                grid.RemoveFromSnapGridCell(Position, Offset, this);
                IsSet = false;
            }
        }

        void OnTransformMove(object sender, object eventArgs) => UpdatePosition();

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


        public string GetDebugString()
        {
            return $"ofs/pos: {Offset}/{Position}";
        }

        MapIndices SnapGridPosAt(Direction dir)
        {
            switch (dir)
            {
                case Direction.East:
                    return Position + new MapIndices(1, 0);
                case Direction.SouthEast:
                    return Position + new MapIndices(1, -1);
                case Direction.South:
                    return Position + new MapIndices(0, -1);
                case Direction.SouthWest:
                    return Position + new MapIndices(-1, -1);
                case Direction.West:
                    return Position + new MapIndices(-1, 0);
                case Direction.NorthWest:
                    return Position + new MapIndices(-1, 1);
                case Direction.North:
                    return Position + new MapIndices(0, 1);
                case Direction.NorthEast:
                    return Position + new MapIndices(1, 1);
                default:
                    throw new NotImplementedException();
            }
        }

        private void UpdatePosition()
        {
            if (!_mapManager.TryGetGrid(Owner.Transform.GridID, out var grid))
            {
                Logger.WarningS(LogCategory, "Entity {0} snapgrid didn't find grid {1}. Race condition?", Owner.Uid, Owner.Transform.GridID);
                return;
            }

            if (IsSet)
            {
                grid.RemoveFromSnapGridCell(Position, Offset, this);
            }

            IsSet = true;

            var oldPos = Position;
            Position = grid.SnapGridCellFor(Owner.Transform.GridPosition, Offset);
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
