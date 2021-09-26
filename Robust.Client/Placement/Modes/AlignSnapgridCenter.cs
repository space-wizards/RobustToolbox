using System;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Placement.Modes
{
    public class SnapgridCenter : PlacementMode
    {
        private IMapGrid? _grid;
        float _snapSize;

        public override bool HasLineMode => true;
        public override bool HasGridMode => true;

        public SnapgridCenter(PlacementManager pMan) : base(pMan) { }

        public override void Render(DrawingHandleWorld handle)
        {
            if (_grid != null)
            {
                var viewportSize = (Vector2)pManager._clyde.ScreenSize;

                var gridPosition = _grid.MapToGrid(pManager.eyeManager.ScreenToMap(Vector2.Zero));
                var position = gridPosition.WithPosition(new Vector2(MathF.Floor(gridPosition.X), MathF.Floor(gridPosition.Y))).ToMap(IoCManager.Resolve<IEntityManager>());

                var gridstart = pManager.eyeManager.WorldToScreen(new Vector2( //Find snap grid closest to screen origin and convert back to screen coords
                    (float)(MathF.Round(position.X / _snapSize - 0.5f, MidpointRounding.AwayFromZero) + 0.5f) * _snapSize,
                    (float)(MathF.Round(position.Y / _snapSize - 0.5f, MidpointRounding.AwayFromZero) + 0.5f) * _snapSize));
                for (var a = gridstart.X; a < viewportSize.X; a += _snapSize * 32) //Iterate through screen creating gridlines
                {
                    var from = ScreenToWorld(new Vector2(a, 0));
                    var to = ScreenToWorld(new Vector2(a, viewportSize.Y));
                    handle.DrawLine(from, to, new Color(0, 0, 1f));
                }
                for (var a = gridstart.Y; a < viewportSize.Y; a += _snapSize * 32)
                {
                    var from = ScreenToWorld(new Vector2(0, a));
                    var to = ScreenToWorld(new Vector2(viewportSize.X, a));
                    handle.DrawLine(from, to, new Color(0, 0, 1f));
                }
            }

            // Draw grid BELOW the ghost thing.
            base.Render(handle);
        }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);

            var gridId = MouseCoords.GetGridId(pManager.EntityManager);
            _snapSize = 1f;
            if (gridId.IsValid())
            {
                _grid = pManager.MapManager.GetGrid(gridId);
                _snapSize = _grid.TileSize; //Find snap size for the grid.
            }
            else
            {
                _grid = null;
            }

            GridDistancing = _snapSize;

            var mouseLocal = new Vector2( //Round local coordinates onto the snap grid
                (float)(MathF.Round((MouseCoords.Position.X / _snapSize - 0.5f), MidpointRounding.AwayFromZero) + 0.5) * _snapSize,
                (float)(MathF.Round((MouseCoords.Position.Y / _snapSize - 0.5f), MidpointRounding.AwayFromZero) + 0.5) * _snapSize);

            //Adjust mouseCoords to new calculated position
            MouseCoords = new EntityCoordinates(MouseCoords.EntityId, mouseLocal + new Vector2(pManager.PlacementOffset.X, pManager.PlacementOffset.Y));
        }

        public override bool IsValidPosition(EntityCoordinates position)
        {
            if (!RangeCheck(position))
            {
                return false;
            }

            return true;
        }
    }
}
