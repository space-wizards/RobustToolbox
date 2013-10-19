using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GameObject;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.Chat;
using ServerInterfaces.Map;
using ServerServices.Tiles;

namespace SGO
{
    public class BasicDoorComponent : BasicLargeObjectComponent
    {
        private bool Open;
        private bool autoclose = true;
        private string closedSprite = "";
        private bool disabled;
        private float openLength = 5000;
        private string openSprite = "";
        private bool openonbump;
        private float timeOpen;

        public BasicDoorComponent()
        {
            Family = ComponentFamily.LargeObject;

            RegisterSVar("OpenOnBump", typeof (bool));
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

            if (disabled) return;

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
            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove += OnMove;
        }

        public override void OnRemove()
        {
            Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove -= OnMove;
            base.OnRemove();
        }

        private void OnMove(object sender, VectorEventArgs args)
        {
            SetPermeable(args.VectorFrom);
            SetImpermeable(args.VectorTo);
        }

        protected override void RecieveItemInteraction(Entity actor, Entity item,
                                                       Lookup<ItemCapabilityType, ItemCapabilityVerb> verbs)
        {
            base.RecieveItemInteraction(actor, item, verbs);

            if (verbs[ItemCapabilityType.Tool].Contains(ItemCapabilityVerb.Pry))
            {
                ToggleDoor(true);
            }
            else if (verbs[ItemCapabilityType.Tool].Contains(ItemCapabilityVerb.Hit))
            {
                var cm = IoCManager.Resolve<IChatManager>();
                cm.SendChatMessage(ChatChannel.Default,
                                   actor.Name + " hit the " + Owner.Name + " with a " + item.Name + ".", null, item.Uid);
            }
            else if (verbs[ItemCapabilityType.Tool].Contains(ItemCapabilityVerb.Emag))
            {
                OpenDoor();
                disabled = true;
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

        private void ToggleDoor(bool forceToggle = false)
        {
            //Apply actions
            if (Open)
            {
                CloseDoor(forceToggle);
            }
            else
            {
                OpenDoor(forceToggle);
            }
        }

        private void OpenDoor(bool force = false)
        {
            if (disabled && !force) return;

            var map = IoCManager.Resolve<IMapManager>();
            Tile t = (Tile)map.GetITileAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            Open = true;
            Owner.SendMessage(this, ComponentMessageType.DisableCollision);
            Owner.SendMessage(this, ComponentMessageType.SetSpriteByKey, openSprite);
            if(t != null)
                t.GasPermeable = true;
        }

        private void CloseDoor(bool force = false)
        {
            if (disabled && !force) return;

            var map = IoCManager.Resolve<IMapManager>();
            Tile t = (Tile)map.GetITileAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            Open = true;
            Open = false;
            timeOpen = 0;
            Owner.SendMessage(this, ComponentMessageType.EnableCollision);
            Owner.SendMessage(this, ComponentMessageType.SetSpriteByKey, closedSprite);
            if (t != null)
                t.GasPermeable = false;
        }

        private void SetImpermeable()
        {
            var map = IoCManager.Resolve<IMapManager>();
            Tile t = (Tile)map.GetITileAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            if (t != null)
                t.GasPermeable = false;
        }

        private void SetImpermeable(Vector2 position)
        {
            var map = IoCManager.Resolve<IMapManager>();
            Tile t = (Tile)map.GetITileAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            if (t != null)
                t.GasPermeable = false;
        }

        private void SetPermeable(Vector2 position)
        {
            var map = IoCManager.Resolve<IMapManager>();
            Tile t = (Tile)map.GetITileAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            if (t != null)
                t.GasPermeable = true;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "OpenSprite":
                    openSprite = parameter.GetValue<string>();
                    break;
                case "ClosedSprite":
                    closedSprite = parameter.GetValue<string>();
                    break;
                case "OpenOnBump":
                    openonbump = parameter.GetValue<bool>();
                    break;
                case "AutoCloseInterval":
                    var autocloseinterval = parameter.GetValue<int>();
                    if (autocloseinterval == 0)
                        autoclose = false;
                    else
                    {
                        autoclose = true;
                        openLength = autocloseinterval;
                    }
                    break;
                default:
                    base.SetParameter(parameter);
                    break;
            }
        }

        public override List<ComponentParameter> GetParameters()
        {
            List<ComponentParameter> cparams = base.GetParameters();
            cparams.Add(new ComponentParameter("OpenOnBump", openonbump));
            return cparams;
        }
    }
}