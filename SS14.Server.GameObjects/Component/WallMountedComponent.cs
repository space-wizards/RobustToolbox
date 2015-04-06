using SS14.Server.Interfaces.GOC;
using SS14.Server.Interfaces.Map;
using SS14.Server.Services.Tiles;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using System;
using SS14.Shared.Maths;

namespace SS14.Server.GameObjects
{
    public class WallMountedComponent : Component, IWallMountedComponent
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
            var pos = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position;
            var tileSpace = map.GetTileSpacing();

            switch (Owner.GetComponent<DirectionComponent>(ComponentFamily.Direction).Direction)
            {
                case Direction.South:
                    AttachToTile(pos);
                    break;
                case Direction.North:
                    AttachToTile(new Vector2(pos.X, pos.Y + (2 * tileSpace)));
                    break;
                case Direction.East:
                    AttachToTile(new Vector2(pos.X - tileSpace, pos.Y + tileSpace));
                    break;
                case Direction.West:
                    AttachToTile(new Vector2(pos.X + tileSpace, pos.Y + tileSpace));
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

            var previousTile = map.GetWallAt(args.VectorFrom) as Tile;
            if(previousTile != null)
                previousTile.TileChange -= TileChanged;

            var currentTile = map.GetWallAt(args.VectorTo) as Tile;

            if (currentTile == null) return;

            currentTile.TileChange += TileChanged;

            linkedTile = currentTile;
        }

        public void AttachToTile(Vector2 tilePos)
        {
            var map = IoCManager.Resolve<IMapManager>();

            var currentTile = map.GetWallAt(tilePos) as Tile;

            if (currentTile == null) return;

            var previousTile = map.GetWallAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position) as Tile;

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