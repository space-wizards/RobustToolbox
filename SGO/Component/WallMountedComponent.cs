using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using SS13_Shared.GO;
using ServerServices.Map;
using ServerServices;
using ServerInterfaces;

namespace SGO
{
    public class WallMountedComponent : GameObjectComponent
    {
        protected ServerServices.Tiles.Tile linkedTile;

        public WallMountedComponent()
            :base()
        {
            family = SS13_Shared.GO.ComponentFamily.WallMounted;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }

        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);
            Owner.OnMove += new Entity.EntityMoveEvent(OnMove);
        }

        public override void OnRemove()
        {
            Owner.OnMove -= new Entity.EntityMoveEvent(OnMove);
            base.OnRemove();
        }

        private void OnMove(Vector2 newPosition, Vector2 oldPosition)
        {
            Map map = (Map)ServiceManager.Singleton.GetService(ServerServiceType.Map);

            System.Drawing.Point tilePositionOld = map.GetTileArrayPositionFromWorldPosition(oldPosition);
            ServerServices.Tiles.Tile previousTile = map.GetTileAt(tilePositionOld.X, tilePositionOld.Y);

            previousTile.TileChange -= new ServerServices.Tiles.Tile.TileChangeHandler(TileChanged);

            System.Drawing.Point tilePositionNew = map.GetTileArrayPositionFromWorldPosition(newPosition);
            ServerServices.Tiles.Tile currentTile = map.GetTileAt(tilePositionNew.X, tilePositionNew.Y);

            currentTile.TileChange += new ServerServices.Tiles.Tile.TileChangeHandler(TileChanged);

            linkedTile = currentTile;
        }

        protected virtual void TileChanged(TileType tNew)
        {
            if (tNew != TileType.Wall)
            {
                Owner.Translate(Owner.position + new Vector2(0, 64), 90);
            }
        }
    }
}
