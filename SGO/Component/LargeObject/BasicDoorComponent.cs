using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.GO;
using ServerServices.Map;
using ServerServices;
using ServerInterfaces;
using SS13_Shared.HelperClasses;

namespace SGO
{
    public class BasicDoorComponent : BasicLargeObjectComponent
    {
        bool Open = false;
        string openSprite = "";
        string closedSprite = "";
        float openLength = 5000;
        float timeOpen = 0;
        bool openonbump = false;
        bool autoclose = true;

        public BasicDoorComponent()
            :base()
        {
            family = SS13_Shared.GO.ComponentFamily.LargeObject;
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            base.RecieveMessage(sender, type, replies, list);

            switch(type)
            {
                case ComponentMessageType.Bumped:
                    if(openonbump)
                        OpenDoor();
                    break;

            }
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
            Owner.OnMove += new Entity.EntityMoveEvent(OnMove);
        }

        public override void OnRemove()
        {
            Owner.OnMove -= new Entity.EntityMoveEvent(OnMove);
            base.OnRemove();
        }

        private void OnMove(Vector2 newPosition, Vector2 oldPosition)
        {
            SetPermeable(oldPosition);
            SetImpermeable(newPosition);
        }

        protected override void RecieveItemInteraction(Entity actor, Entity item, Lookup<ItemCapabilityType, ItemCapabilityVerb> verbs)
        {
            base.RecieveItemInteraction(actor, item, verbs);

            if (verbs[ItemCapabilityType.Tool].Contains(ItemCapabilityVerb.Pry))
            {
                ToggleDoor();
            }
            else if (verbs[ItemCapabilityType.Tool].Contains(ItemCapabilityVerb.Hit))
            {
                IChatManager cm = (IChatManager)ServiceManager.Singleton.GetService(ServerServiceType.ChatManager);
                cm.SendChatMessage(ChatChannel.Default, actor.name + " hit the " + Owner.name + " with a " + item.name + ".", null, item.Uid);
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
            Map map = (Map)ServiceManager.Singleton.GetService(ServerServiceType.Map);
            var occupiedTilePos = map.GetTileArrayPositionFromWorldPosition(Owner.position);
            var occupiedTile = map.GetTileAt(occupiedTilePos.X, occupiedTilePos.Y);
            Open = true;
            Owner.SendMessage(this, ComponentMessageType.DisableCollision, null);
            Owner.SendMessage(this, ComponentMessageType.SetSpriteByKey, null, openSprite);
            occupiedTile.gasPermeable = true;
            occupiedTile.gasCell.blocking = false;
        }

        private void CloseDoor()
        {
            Map map = (Map)ServiceManager.Singleton.GetService(ServerServiceType.Map);
            var occupiedTilePos = map.GetTileArrayPositionFromWorldPosition(Owner.position);
            var occupiedTile = map.GetTileAt(occupiedTilePos.X, occupiedTilePos.Y);
            Open = false;
            timeOpen = 0;
            Owner.SendMessage(this, ComponentMessageType.EnableCollision, null);
            Owner.SendMessage(this, ComponentMessageType.SetSpriteByKey, null, closedSprite);
            occupiedTile.gasPermeable = false;
            occupiedTile.gasCell.blocking = true;
        }

        private void SetImpermeable()
        {
            Map map = (Map)ServiceManager.Singleton.GetService(ServerServiceType.Map);
            var occupiedTilePos = map.GetTileArrayPositionFromWorldPosition(Owner.position);
            var occupiedTile = map.GetTileAt(occupiedTilePos.X, occupiedTilePos.Y);
            occupiedTile.gasPermeable = false;
            occupiedTile.gasCell.blocking = true;
        }

        private void SetImpermeable(Vector2 position)
        {
            Map map = (Map)ServiceManager.Singleton.GetService(ServerServiceType.Map);
            var occupiedTilePos = map.GetTileArrayPositionFromWorldPosition(position);
            var occupiedTile = map.GetTileAt(occupiedTilePos.X, occupiedTilePos.Y);
            occupiedTile.gasPermeable = false;
            occupiedTile.gasCell.blocking = true;
        }

        private void SetPermeable(Vector2 position)
        {
            Map map = (Map)ServiceManager.Singleton.GetService(ServerServiceType.Map);
            var occupiedTilePos = map.GetTileArrayPositionFromWorldPosition(position);
            var occupiedTile = map.GetTileAt(occupiedTilePos.X, occupiedTilePos.Y);
            occupiedTile.gasPermeable = true;
            occupiedTile.gasCell.blocking = false;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "OpenSprite":
                    openSprite = (string)parameter.Parameter;
                    break;
                case "ClosedSprite":
                    closedSprite = (string)parameter.Parameter;
                    break;
                case "OpenOnBump":
                    if ((string)parameter.Parameter == "true")
                        openonbump = true;
                    else
                        openonbump = false;
                    break;
                case "AutoCloseInterval":
                    int autocloseinterval;
                    if (int.TryParse((string)parameter.Parameter, out autocloseinterval))
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
