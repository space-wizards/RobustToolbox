using System;
using System.Drawing;
using GameObject;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.Map;
using ServerServices.Tiles;

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
            Point arrayPos =
                map.GetTileArrayPositionFromWorldPosition(
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);

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

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.WallMountTile: //Attach to a specific tile.
                    var toAttach = (Vector2) list[0];
                    AttachToTile(toAttach);
                    break;
                case ComponentMessageType.WallMountSearch:
                    //Look for the best tile to attach to based on owning entity's direction.
                    FindTile();
                    break;
            }

            return reply;
        }

        private void OnMove(object sender, VectorEventArgs args)
        {
            var map = IoCManager.Resolve<IMapManager>();

            Point tilePositionOld = map.GetTileArrayPositionFromWorldPosition(args.VectorFrom);
            var previousTile = map.GetTileFromIndex(tilePositionOld.X, tilePositionOld.Y) as Tile;

            previousTile.TileChange -= TileChanged;

            Point tilePositionNew = map.GetTileArrayPositionFromWorldPosition(args.VectorTo);
            var currentTile = map.GetTileFromIndex(tilePositionNew.X, tilePositionNew.Y) as Tile;

            if (currentTile == null) return;

            currentTile.TileChange += TileChanged;

            linkedTile = currentTile;
        }

        public void AttachToTile(Vector2 tilePos)
        {
            var map = IoCManager.Resolve<IMapManager>();

            var currentTile = map.GetTileFromIndex((int) tilePos.X, (int) tilePos.Y) as Tile;

            if (currentTile == null) return;

            Point tilePositionOld =
                map.GetTileArrayPositionFromWorldPosition(
                    Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            var previousTile = map.GetTileFromIndex(tilePositionOld.X, tilePositionOld.Y) as Tile;

            previousTile.TileChange -= TileChanged;

            currentTile.TileChange += TileChanged;

            linkedTile = currentTile;
        }

        protected virtual void TileChanged(Type tNew)
        {
            if (tNew != typeof (Wall))
            {
                Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).TranslateByOffset(new Vector2(0, 64));
            }
        }
    }
}