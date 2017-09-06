using OpenTK;
using SFML.Graphics;
using SFML.System;
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

            mouseScreen = mouseS;
            mouseCoords = CluwneLib.ScreenToCoordinates(mouseScreen);

            if (pManager.CurrentPermission.IsTile)
                return false;

            currentTile = mouseCoords.Grid.GetTile(mouseCoords);

            if (!RangeCheck())
                return false;

            var nodes = new List<Vector2>();

            if (pManager.CurrentPrototype.MountingPoints != null)
            {
                nodes.AddRange(
                    pManager.CurrentPrototype.MountingPoints.Select(
                        current => new Vector2(mouseCoords.X, currentTile.Y + current)));
            }
            else
            {
                nodes.Add(new Vector2(mouseCoords.X, currentTile.Y + 0.5f));
                nodes.Add(new Vector2(mouseCoords.X, currentTile.Y + 1.0f));
                nodes.Add(new Vector2(mouseCoords.X, currentTile.Y + 1.5f));
            }

            Vector2 closestNode = (from Vector2 node in nodes
                                    orderby (node - mouseCoords).LengthSquared ascending
                                    select node).First();

            mouseCoords = closestNode + new Vector2(pManager.CurrentPrototype.PlacementOffset.X,
                                                    pManager.CurrentPrototype.PlacementOffset.Y);
            mouseScreen = CluwneLib.WorldToScreen(mouseCoords);

            return true;
        }
    }
}
