using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Equippable;
using System;

namespace SS14.Server.GameObjects
{
    public class EquippableComponent : Component
    {
        public EquipmentSlot wearloc;

        public Entity currentWearer { get; set; }

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

        public override void SetParameter(ComponentParameter parameter)
        {
            base.SetParameter(parameter);

            switch (parameter.MemberName)
            {
                case "wearloc":
                    wearloc = (EquipmentSlot) Enum.Parse(typeof (EquipmentSlot), parameter.GetValue<string>());
                    break;
            }
        }

        public override ComponentState GetComponentState()
        {
            return new EquippableComponentState(wearloc, currentWearer != null ? currentWearer.Uid : (int?)null);
        }
    }
}
