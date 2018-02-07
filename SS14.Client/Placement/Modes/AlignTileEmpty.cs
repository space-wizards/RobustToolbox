using SS14.Client.Graphics;
using SS14.Shared.IoC;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Placement.Modes
{
    public class AlignTileEmpty : PlacementMode
    {
        public AlignTileEmpty(PlacementManager pMan) : base(pMan)
        {
        }

        public override bool FrameUpdate(RenderFrameEventArgs e, ScreenCoordinates mouseS)
        {
            if (mouseS.MapID == MapId.Nullspace) return false;

            MouseScreen = mouseS;
            MouseCoords = pManager.eyeManager.ScreenToWorld(MouseScreen);

            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);
            var tilesize = MouseCoords.Grid.TileSize;

            if (!RangeCheck())
                return false;

            var entitymanager = IoCManager.Resolve<IClientEntityManager>();
            var failtoplace = !entitymanager.AnyEntitiesIntersecting(MouseCoords.MapID, new Box2(new Vector2(CurrentTile.X, CurrentTile.Y), new Vector2(CurrentTile.X + 0.99f, CurrentTile.Y + 0.99f)));

            if (pManager.CurrentPermission.IsTile)
            {
                MouseCoords = new LocalCoordinates(CurrentTile.X + tilesize/2,
                                                  CurrentTile.Y + tilesize/2,
                                                  MouseCoords.Grid);
            }
            else
            {
                MouseCoords = new LocalCoordinates(CurrentTile.X + tilesize/2 + pManager.CurrentPrototype.PlacementOffset.X,
                                                  CurrentTile.Y + tilesize/2 + pManager.CurrentPrototype.PlacementOffset.Y,
                                                  MouseCoords.Grid);
            }
            MouseScreen = pManager.eyeManager.WorldToScreen(MouseCoords);

            return failtoplace;
        }
    }
}
