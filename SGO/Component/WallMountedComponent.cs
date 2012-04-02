using System.Drawing;
using SS13_Shared;
using SS13_Shared.GO;
using ServerServices;
using ServerServices.Map;
using ServerServices.Tiles;

namespace SGO
{
    public class WallMountedComponent : GameObjectComponent
    {
        protected Tile linkedTile;

        public WallMountedComponent()
        {
            family = ComponentFamily.WallMounted;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
        }

        public override void OnAdd(Entity owner)
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
            var map = (Map) ServiceManager.Singleton.GetService(ServerServiceType.Map);

            Point tilePositionOld = map.GetTileArrayPositionFromWorldPosition(oldPosition);
            Tile previousTile = map.GetTileAt(tilePositionOld.X, tilePositionOld.Y);

            previousTile.TileChange -= TileChanged;

            Point tilePositionNew = map.GetTileArrayPositionFromWorldPosition(newPosition);
            Tile currentTile = map.GetTileAt(tilePositionNew.X, tilePositionNew.Y);

            currentTile.TileChange += TileChanged;

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