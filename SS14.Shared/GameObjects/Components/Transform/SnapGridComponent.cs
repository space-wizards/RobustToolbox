using System;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;

namespace SS14.Shared.GameObjects.Components.Transform
{
    /// <summary>
    ///     Makes it possible to look this entity up with the snap grid.
    /// </summary>
    public class SnapGridComponent : Component
    {
        public const string LogCategory = "go.comp.snapgrid";
        public sealed override string Name => "SnapGrid";

        private bool IsSet;
        private SnapGridOffset _offset = SnapGridOffset.Center;

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
                var mapMan = IoCManager.Resolve<IMapManager>();
                if (!mapMan.TryGetGrid(Owner.Transform.GridID, out var grid))
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

        private void UpdatePosition()
        {
            var mapMan = IoCManager.Resolve<IMapManager>();
            if (!mapMan.TryGetGrid(Owner.Transform.GridID, out var grid))
            {
                Logger.WarningS(LogCategory, "Entity {0} snapgrid didn't find grid {1}. Race condition?", Owner.Uid, Owner.Transform.GridID);
                return;
            }

            if (IsSet)
            {
                grid.RemoveFromSnapGridCell(Position, Offset, this);
            }

            IsSet = true;

            Position = grid.SnapGridCellFor(Owner.Transform.LocalPosition, Offset);
            grid.AddToSnapGridCell(Position, Offset, this);

            Logger.InfoS(LogCategory, "We in there at {0}", Position);
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
