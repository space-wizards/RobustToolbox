using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Equippable;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    [IoCTarget]
    public class EquippableComponent : Component
    {
        public override string Name => "Equippable";
        public EquipmentSlot wearloc;

        public IEntity currentWearer { get; set; }

        public EquippableComponent()
        {
            Family = ComponentFamily.Equippable;
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.GetWearLoc:
                    reply = new ComponentReplyMessage(ComponentMessageType.ReturnWearLoc, wearloc);
                    break;
            }

            return reply;
        }

        public override void LoadParameters(Dictionary<string, YamlNode> mapping)
        {
            if (mapping.TryGetValue("wearloc", out YamlNode node))
            {
                wearloc = node.AsEnum<EquipmentSlot>();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new EquippableComponentState(wearloc, currentWearer?.Uid);
        }
    }
}
