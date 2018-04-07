using SS14.Client.Graphics;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Placement.Modes
{
    public class AlignWall : PlacementMode
    {
        public AlignWall(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool FrameUpdate(RenderFrameEventArgs e, ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapId.Nullspace) return false;

            MouseScreen = mouseS;
            MouseCoords = pManager.eyeManager.ScreenToWorld(MouseScreen);

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
            MouseScreen = pManager.eyeManager.WorldToScreen(MouseCoords);

            return true;
        }
    }
}
