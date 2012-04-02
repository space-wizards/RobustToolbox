using System.Drawing;
using System.Linq;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces;
using ServerServices;
using ServerServices.Map;
using ServerServices.Tiles;

namespace SGO
{
    public class BasicDoorComponent : BasicLargeObjectComponent
    {
        private bool Open;
        private bool autoclose = true;
        private string closedSprite = "";
        private float openLength = 5000;
        private string openSprite = "";
        private bool openonbump;
        private float timeOpen;

        public BasicDoorComponent()
        {
            family = ComponentFamily.LargeObject;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.Bumped:
                    if (openonbump)
                        OpenDoor();
                    break;
            }

            return reply;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (Open && autoclose)
            {
                timeOpen += frameTime;
                if (timeOpen >= openLength)
                    CloseDoor();
            }
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
            SetPermeable(oldPosition);
            SetImpermeable(newPosition);
        }

        protected override void RecieveItemInteraction(Entity actor, Entity item,
                                                       Lookup<ItemCapabilityType, ItemCapabilityVerb> verbs)
        {
            base.RecieveItemInteraction(actor, item, verbs);

            if (verbs[ItemCapabilityType.Tool].Contains(ItemCapabilityVerb.Pry))
            {
                ToggleDoor();
            }
            else if (verbs[ItemCapabilityType.Tool].Contains(ItemCapabilityVerb.Hit))
            {
                var cm = (IChatManager) ServiceManager.Singleton.GetService(ServerServiceType.ChatManager);
                cm.SendChatMessage(ChatChannel.Default,
                                   actor.Name + " hit the " + Owner.Name + " with a " + item.Name + ".", null, item.Uid);
            }
        }

        /// <summary>
        /// Entry point for interactions between an empty hand and this object
        /// Basically, actor "uses" this object
        /// </summary>
        /// <param name="actor">The actor entity</param>
        protected override void HandleEmptyHandToLargeObjectInteraction(Entity actor)
        {
            ToggleDoor();
        }

        private void ToggleDoor()
        {
            //Apply actions
            if (Open)
            {
                CloseDoor();
            }
            else
            {
                OpenDoor();
            }
        }

        private void OpenDoor()
        {
            var map = (Map) ServiceManager.Singleton.GetService(ServerServiceType.Map);
            Point occupiedTilePos = map.GetTileArrayPositionFromWorldPosition(Owner.position);
            Tile occupiedTile = map.GetTileAt(occupiedTilePos.X, occupiedTilePos.Y);
            Open = true;
            Owner.SendMessage(this, ComponentMessageType.DisableCollision);
            Owner.SendMessage(this, ComponentMessageType.SetSpriteByKey, openSprite);
            occupiedTile.gasPermeable = true;
            occupiedTile.gasCell.blocking = false;
        }

        private void CloseDoor()
        {
            var map = (Map) ServiceManager.Singleton.GetService(ServerServiceType.Map);
            Point occupiedTilePos = map.GetTileArrayPositionFromWorldPosition(Owner.position);
            Tile occupiedTile = map.GetTileAt(occupiedTilePos.X, occupiedTilePos.Y);
            Open = false;
            timeOpen = 0;
            Owner.SendMessage(this, ComponentMessageType.EnableCollision);
            Owner.SendMessage(this, ComponentMessageType.SetSpriteByKey, closedSprite);
            occupiedTile.gasPermeable = false;
            occupiedTile.gasCell.blocking = true;
        }

        private void SetImpermeable()
        {
            var map = (Map) ServiceManager.Singleton.GetService(ServerServiceType.Map);
            Point occupiedTilePos = map.GetTileArrayPositionFromWorldPosition(Owner.position);
            Tile occupiedTile = map.GetTileAt(occupiedTilePos.X, occupiedTilePos.Y);
            occupiedTile.gasPermeable = false;
            occupiedTile.gasCell.blocking = true;
        }

        private void SetImpermeable(Vector2 position)
        {
            var map = (Map) ServiceManager.Singleton.GetService(ServerServiceType.Map);
            Point occupiedTilePos = map.GetTileArrayPositionFromWorldPosition(position);
            Tile occupiedTile = map.GetTileAt(occupiedTilePos.X, occupiedTilePos.Y);
            occupiedTile.gasPermeable = false;
            occupiedTile.gasCell.blocking = true;
        }

        private void SetPermeable(Vector2 position)
        {
            var map = (Map) ServiceManager.Singleton.GetService(ServerServiceType.Map);
            Point occupiedTilePos = map.GetTileArrayPositionFromWorldPosition(position);
            Tile occupiedTile = map.GetTileAt(occupiedTilePos.X, occupiedTilePos.Y);
            occupiedTile.gasPermeable = true;
            occupiedTile.gasCell.blocking = false;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "OpenSprite":
                    openSprite = (string) parameter.Parameter;
                    break;
                case "ClosedSprite":
                    closedSprite = (string) parameter.Parameter;
                    break;
                case "OpenOnBump":
                    if ((string) parameter.Parameter == "true")
                        openonbump = true;
                    else
                        openonbump = false;
                    break;
                case "AutoCloseInterval":
                    int autocloseinterval;
                    if (int.TryParse((string) parameter.Parameter, out autocloseinterval))
                    {
                        if (autocloseinterval == 0)
                            autoclose = false;
                        else
                        {
                            autoclose = true;
                            openLength = autocloseinterval;
                        }
                    }
                    break;
                default:
                    base.SetParameter(parameter);
                    break;
            }
        }
    }
}