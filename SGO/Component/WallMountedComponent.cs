using System.Drawing;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.GameObject;
using ServerServices;
using ServerServices.Map;
using ServerServices.Tiles;
using SS13.IoC;
using ServerInterfaces.Map;
using Lidgren.Network;

namespace SGO
{
    public class WallMountedComponent : GameObjectComponent
    {
        protected Tile linkedTile;

        public WallMountedComponent()
        {
            family = ComponentFamily.WallMounted;
        }
        
        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            Owner.OnMove += OnMove;
        }

        public override void OnRemove()
        {
            Owner.OnMove -= OnMove;
            base.OnRemove();
        }

        private void OnMove(Vector2 newPosition, Vector2 oldPosition)
        {
            var map = IoCManager.Resolve<IMap>();

            Point tilePositionOld = map.GetTileArrayPositionFromWorldPosition(oldPosition);
            Tile previousTile = map.GetTileAt(tilePositionOld.X, tilePositionOld.Y) as Tile;

            previousTile.TileChange -= TileChanged;

            Point tilePositionNew = map.GetTileArrayPositionFromWorldPosition(newPosition);
            Tile currentTile = map.GetTileAt(tilePositionNew.X, tilePositionNew.Y) as Tile;

            currentTile.TileChange += TileChanged;

            linkedTile = currentTile;
        }

        public void AttachToTile(Vector2 tilePos)
        {
            var map = IoCManager.Resolve<IMap>();

            Point tilePositionOld = map.GetTileArrayPositionFromWorldPosition(Owner.Position);
            Tile previousTile = map.GetTileAt(tilePositionOld.X, tilePositionOld.Y) as Tile;

            previousTile.TileChange -= TileChanged;

            Tile currentTile = map.GetTileAt((int)tilePos.X, (int)tilePos.Y) as Tile;

            currentTile.TileChange += TileChanged;

            linkedTile = currentTile;
        }

        protected virtual void TileChanged(TileType tNew)
        {
            if (tNew != TileType.Wall)
            {
                Owner.Translate(Owner.Position + new Vector2(0, 64), 90);
            }
        }
    }
}