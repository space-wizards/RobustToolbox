using System.Drawing;
using GameObject;
using SS13_Shared;
using SS13_Shared.GO;
using ServerServices.Tiles;
using SS13.IoC;
using ServerInterfaces.Map;
using System;

namespace SGO
{
    public class WallMountedComponent : Component
    {
        protected Tile linkedTile;

        public WallMountedComponent()
        {
            Family = ComponentFamily.WallMounted;
        }
        
        public override void OnAdd(Entity owner)
        {
            base.OnAdd(owner);
            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove += OnMove;
        }

        public override void OnRemove()
        {
            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove -= OnMove;
            base.OnRemove();
        }

        private void FindTile()
        {
            var map = IoCManager.Resolve<IMapManager>();
            var arrayPos = map.GetTileArrayPositionFromWorldPosition(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);

            switch (Owner.GetComponent<DirectionComponent>(ComponentFamily.Direction).Direction)
            {
                case Direction.South:
                    AttachToTile(new Vector2(arrayPos.X, arrayPos.Y));
                    break;
                case Direction.North:
                    AttachToTile(new Vector2(arrayPos.X, arrayPos.Y + 2));
                    break;
                case Direction.East:
                    AttachToTile(new Vector2(arrayPos.X - 1, arrayPos.Y + 1));
                    break;
                case Direction.West:
                    AttachToTile(new Vector2(arrayPos.X + 1, arrayPos.Y + 1));
                    break;
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.WallMountTile: //Attach to a specific tile.
                    Vector2 toAttach = (Vector2)list[0];
                    AttachToTile(toAttach);
                    break;
                case ComponentMessageType.WallMountSearch: //Look for the best tile to attach to based on owning entity's direction.
                    FindTile();
                    break;
            }

            return reply;
        }

        private void OnMove(object sender, VectorEventArgs args)
        {
            var map = IoCManager.Resolve<IMapManager>();

            Point tilePositionOld = map.GetTileArrayPositionFromWorldPosition(args.VectorFrom);
            Tile previousTile = map.GetTileAt(tilePositionOld.X, tilePositionOld.Y) as Tile;

            previousTile.TileChange -= TileChanged;

            Point tilePositionNew = map.GetTileArrayPositionFromWorldPosition(args.VectorTo);
            Tile currentTile = map.GetTileAt(tilePositionNew.X, tilePositionNew.Y) as Tile;

            currentTile.TileChange += TileChanged;

            linkedTile = currentTile;
        }

        public void AttachToTile(Vector2 tilePos)
        {
            var map = IoCManager.Resolve<IMapManager>();

            Tile currentTile = map.GetTileAt((int)tilePos.X, (int)tilePos.Y) as Tile;

            if (currentTile == null) return;

            Point tilePositionOld = map.GetTileArrayPositionFromWorldPosition(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            Tile previousTile = map.GetTileAt(tilePositionOld.X, tilePositionOld.Y) as Tile;

            previousTile.TileChange -= TileChanged;

            currentTile.TileChange += TileChanged;

            linkedTile = currentTile;
        }

        protected virtual void TileChanged(Type tNew)
        {
            if (tNew != typeof(Wall))
            {
                Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).TranslateByOffset(new Vector2(0, 64));
            }
        }
    }
}