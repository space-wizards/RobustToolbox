using SFML.System;
using SS14.Server.Interfaces.GOC;
using SS14.Server.Interfaces.Map;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects
{
    public class WallMountedComponent : Component, IWallMountedComponent
    {
        public WallMountedComponent()
        {
            Family = ComponentFamily.WallMounted;
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
                    var toAttach = (Vector2f) list[0];
                    var tile = IoCManager.Resolve<IMapManager>().GetTileRef(toAttach);
                    AttachToTile(tile);
                    break;
                //case ComponentMessageType.WallMountSearch:
                //    //Look for the best tile to attach to based on owning entity's direction.
                //    FindTile();
                //    break;
            }

            return reply;
        }

        public void AttachToTile(TileRef tilePos)
        {
            var transComp = Owner.GetComponent<TransformComponent>(ComponentFamily.Transform);
            if (transComp != null)
                transComp.Position = new Vector2f(tilePos.X + 0.5f, tilePos.Y + 0.5f);
        }

    }
}