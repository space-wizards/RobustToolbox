using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Placement.Modes
{
    public class AlignWall : PlacementMode
    {
        public AlignWall(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToPlayerGrid(mouseScreen);
            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);

            if (pManager.CurrentPermission.IsTile)
            {
                return;
            }

            var nodes = new List<Vector2>();

            if (pManager?.CurrentPrototype.MountingPoints != null)
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

            MouseCoords = new GridLocalCoordinates(closestNode + new Vector2(pManager.PlacementOffset.X,
                                                                         pManager.PlacementOffset.Y),
                                               MouseCoords.Grid);
        }

        public override bool IsValidPosition(GridLocalCoordinates position)
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
