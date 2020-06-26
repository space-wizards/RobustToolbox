using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Map;
using Vector2 = Robust.Shared.Maths.Vector2;

namespace Robust.Client.Placement.Modes
{
    public class AlignWall : PlacementMode
    {
        public AlignWall(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToCursorGrid(mouseScreen);
            CurrentTile = pManager.MapManager.GetGrid(MouseCoords.GridID).GetTileRef(MouseCoords);

            if (pManager.CurrentPermission!.IsTile)
            {
                return;
            }

            var nodes = new List<Vector2>();

            if (pManager.CurrentPrototype!.MountingPoints != null)
            {
                nodes.AddRange(
                    pManager.CurrentPrototype.MountingPoints.Select(
                        current => new Vector2(MouseCoords.X, CurrentTile.Y + current)));
            }
            else
            {
                nodes.Add(new Vector2(MouseCoords.X, CurrentTile.Y + 0.5f));
                nodes.Add(new Vector2(MouseCoords.X, CurrentTile.Y + 1.0f));
                nodes.Add(new Vector2(MouseCoords.X, CurrentTile.Y + 1.5f));
            }

            Vector2 closestNode = (from Vector2 node in nodes
                                   orderby (node - MouseCoords.Position).LengthSquared ascending
                                   select node).First();

            MouseCoords = new GridCoordinates(closestNode + new Vector2(pManager.PlacementOffset.X,
                                                                         pManager.PlacementOffset.Y),
                                               MouseCoords.GridID);
        }

        public override bool IsValidPosition(GridCoordinates position)
        {
            if (pManager.CurrentPermission!.IsTile)
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
