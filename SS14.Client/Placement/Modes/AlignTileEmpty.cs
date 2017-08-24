using OpenTK;
using SFML.Graphics;
using SFML.System;
using SS14.Client.GameObjects;
using SS14.Client.Graphics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using Vector2i = SS14.Shared.Maths.Vector2i;
using SS14.Shared.IoC;
using SS14.Client.Interfaces.GameObjects;

namespace SS14.Client.Placement.Modes
{
    public class AlignTileEmpty : PlacementMode
    {
        public AlignTileEmpty(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool Update(Vector2i mouseS, IMapManager currentMap)
        {
            if (currentMap == null) return false;

            mouseScreen = mouseS;
            mouseWorld = CluwneLib.ScreenToWorld(mouseScreen);

            currentTile = currentMap.GetDefaultGrid().GetTile(mouseWorld);

            if (!RangeCheck())
                return false;

            if (pManager.CurrentPermission.IsTile)
            {
                mouseWorld = new Vector2(currentTile.X + 0.5f,
                                         currentTile.Y + 0.5f);
                mouseScreen = (Vector2i)CluwneLib.WorldToScreen(mouseWorld);
            }
            else
            {
                mouseWorld = new Vector2(currentTile.X + 0.5f + pManager.CurrentPrototype.PlacementOffset.X,
                                         currentTile.Y + 0.5f + pManager.CurrentPrototype.PlacementOffset.Y);
                mouseScreen = (Vector2i)CluwneLib.WorldToScreen(mouseWorld);
            }

            var entitymanager = IoCManager.Resolve<IClientEntityManager>();
            if (entitymanager.AnyEntitiesIntersecting(new Box2(new Vector2(currentTile.X - 0.49f, currentTile.Y - 0.49f), new Vector2(currentTile.X + 0.49f, currentTile.Y + 0.49f))))
                return false;
            return true;
        }
    }
}
