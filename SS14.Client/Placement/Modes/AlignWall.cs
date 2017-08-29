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

namespace SS14.Client.Placement.Modes
{
    public class AlignWall : PlacementMode
    {
        public AlignWall(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool Update(ScreenCoordinates mouseS)
        {
            if (currentMap == null) return false;

            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);

            if (pManager.CurrentPermission.IsTile)
                return false;

            currentTile = currentMap.GetDefaultGrid().GetTile(mouseWorld);

            if (!RangeCheck())
                return false;

            var nodes = new List<Vector2>();

            if (pManager.CurrentPrototype.MountingPoints != null)
            {
                nodes.AddRange(
                    pManager.CurrentPrototype.MountingPoints.Select(
                        current => new Vector2(mouseWorld.X, currentTile.Y + current)));
            }
            else
            {
                nodes.Add(new Vector2(mouseWorld.X, currentTile.Y + 0.5f));
                nodes.Add(new Vector2(mouseWorld.X, currentTile.Y + 1.0f));
                nodes.Add(new Vector2(mouseWorld.X, currentTile.Y + 1.5f));
            }

            Vector2 closestNode = (from Vector2 node in nodes
                                    orderby (node - mouseWorld).LengthSquared ascending
                                    select node).First();

            mouseWorld = closestNode + new Vector2(pManager.CurrentPrototype.PlacementOffset.X,
                                                    pManager.CurrentPrototype.PlacementOffset.Y);
            mouseScreen = (Vector2i)CluwneLib.WorldToScreen(mouseWorld);

            return true;
        }
    }
}
