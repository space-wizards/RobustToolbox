using SFML.System;
using SS14.Server.Interfaces.Chat;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    public class BasicDoorComponent : BasicLargeObjectComponent
    {
        public override string Name => "BasicDoor";
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

            RegisterSVar("OpenOnBump", typeof(bool));
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

        public override void OnAdd(IEntity owner)
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

        protected override void RecieveItemInteraction(IEntity actor, IEntity item,
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
        protected override void HandleEmptyHandToLargeObjectInteraction(IEntity actor)
        {
            ToggleDoor(true);
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

            //var map = IoCManager.Resolve<IMapManager>();
            //Tile t = (Tile)map.GetFloorAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            Open = true;
            Owner.SendMessage(this, ComponentMessageType.DisableCollision);
            Owner.SendMessage(this, ComponentMessageType.SetSpriteByKey, openSprite);
            //if(t != null)
            //    t.GasPermeable = true; // Gotta find another way to do this.
        }

        private void CloseDoor(bool force = false)
        {
            if (disabled && !force) return;

            //var map = IoCManager.Resolve<IMapManager>();
            //Tile t = (Tile)map.GetFloorAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            Open = true;
            Open = false;
            timeOpen = 0;
            Owner.SendMessage(this, ComponentMessageType.EnableCollision);
            Owner.SendMessage(this, ComponentMessageType.SetSpriteByKey, closedSprite);
            //if (t != null)
            //    t.GasPermeable = false; // Gotta find another way to do this.
        }

        private void SetImpermeable()
        {
            //var map = IoCManager.Resolve<IMapManager>();
            //Tile t = (Tile)map.GetFloorAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            //if (t != null)
            //    t.GasPermeable = false;
        }

        private void SetImpermeable(Vector2f position)
        {
            //var map = IoCManager.Resolve<IMapManager>();
            //Tile t = (Tile)map.GetFloorAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            //if (t != null)
            //    t.GasPermeable = false;
        }

        private void SetPermeable(Vector2f position)
        {
            //var map = IoCManager.Resolve<IMapManager>();
            //Tile t = (Tile)map.GetFloorAt(Owner.GetComponent<TransformComponent>(ComponentFamily.Transform).Position);
            //if (t != null)
            //    t.GasPermeable = true;
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.TryGetNode("openSprite", out node))
            {
                openSprite = node.AsString();
            }

            if (mapping.TryGetNode("closedSprite", out node))
            {
                closedSprite = node.AsString();
            }

            if (mapping.TryGetNode("openOnBump", out node))
            {
                openonbump = node.AsBool();
            }

            if (mapping.TryGetNode("autoCloseInterval", out node))
            {
                var autocloseinterval = node.AsInt();
                if (autocloseinterval == 0)
                {
                    autoclose = false;
                }
                else
                {
                    autoclose = true;
                    openLength = autocloseinterval;
                }
            }
        }

        public override IList<ComponentParameter> GetParameters()
        {
            IList<ComponentParameter> cparams = base.GetParameters();
            cparams.Add(new ComponentParameter("OpenOnBump", openonbump));
            return cparams;
        }
    }
}
