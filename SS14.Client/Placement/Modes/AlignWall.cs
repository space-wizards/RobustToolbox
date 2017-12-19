using OpenTK;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Utility;
using Vector2i = SS14.Shared.Maths.Vector2i;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Placement.Modes
{
    public class AlignWall : PlacementMode
    {
        public AlignWall(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool Update(ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapManager.NULLSPACE) return false;

            MouseScreen = mouseS;
            MouseCoords = CluwneLib.ScreenToCoordinates(MouseScreen);

            if (pManager.CurrentPermission.IsTile)
                return false;

            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);

            if (!RangeCheck())
                return false;

            var nodes = new List<Vector2>();

            if (pManager.CurrentPrototype.MountingPoints != null)
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

            MouseCoords = new LocalCoordinates(closestNode + new Vector2(pManager.CurrentPrototype.PlacementOffset.X,
                                                                         pManager.CurrentPrototype.PlacementOffset.Y),
                                               MouseCoords.Grid);
            MouseScreen = CluwneLib.WorldToScreen(MouseCoords);

            return true;
        }
    }
}
