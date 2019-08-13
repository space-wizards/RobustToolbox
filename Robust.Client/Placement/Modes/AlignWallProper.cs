using System.Linq;
using Robust.Shared.Map;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Robust.Client.Placement.Modes
{
    /// <summary>
    ///     Snaps in edge on one axis, center in the other.
    /// </summary>
    public class AlignWallProper : PlacementMode
    {
        public AlignWallProper(PlacementManager pMan) : base(pMan)
        {
        }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);
            var grid = pManager.MapManager.GetGrid(MouseCoords.GridID);
            CurrentTile = grid.GetTileRef(MouseCoords);

            if (pManager.CurrentPermission.IsTile)
            {
                return;
            }

            var tileGridCoordinates = grid.GridTileToLocal(CurrentTile.GridIndices);

            var offsets = new Vector2[]
            {
                (0f, 0.5f),
                (0.5f, 0f),
                (0, -0.5f),
                (-0.5f, 0f)
            };

            var closestNode = offsets.Select(o => tileGridCoordinates.Offset(o))
                .OrderBy(node => node.Distance(pManager.MapManager, MouseCoords)).First();

            MouseCoords = closestNode;
        }

        public override bool IsValidPosition(GridCoordinates position)
        {
            if (pManager.CurrentPermission.IsTile)
            {
                return false;
            }
            else if (!RangeCheck(position))
            {
                return false;
            }

            return true;
        }
    }
}
