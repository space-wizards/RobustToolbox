using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;

using System;

namespace SS14.Server.GameObjects.Item.ItemCapability
{
    public class MedicalCapability : ItemCapability
    {
        public int capacity = 100; //Healing capacity
        public DamageType damType = DamageType.Bludgeoning;
        public int healAmount = 10;

        public MedicalCapability()
        {
            CapabilityType = ItemCapabilityType.Medical;
            capabilityName = "MedicalCapability";
            interactsWith = InteractsWith.Actor;
        }

        public override bool ApplyTo(Entity target, Entity sourceActor)
        {
            if (capacity <= 0)
            {
                return false;
                //TODO send the player using the item a message
            }
            var targetedArea = BodyPart.Torso;

            ComponentReplyMessage reply = sourceActor.SendMessage(this, ComponentFamily.Actor,
                                                                  ComponentMessageType.GetActorSession);

            if (reply.MessageType == ComponentMessageType.ReturnActorSession)
            {
                var session = (IPlayerSession) reply.ParamsList[0];
                targetedArea = session.TargetedArea;
            }
            else
                throw new NotImplementedException("Actor has no session or No actor component that returns a session");

            //Reduce the capacity.
            capacity -= 20;

            //Heal the target.
            if (target.HasComponent(ComponentFamily.Damageable))
            {
                target.SendMessage(this, ComponentMessageType.Heal, owner.Owner, 20, damType, targetedArea);

                string sourceName = sourceActor.Name;
                string targetName = (sourceActor.Uid == target.Uid) ? "his" : target.Name + "'s";
                //string suffix = (sourceActor.Uid == target.Uid) ? " What a fucking weirdo..." : "";
                IoCManager.Resolve<IChatManager>()
                    .SendChatMessage(ChatChannel.Damage,
                                     sourceName + " applies the " + owner.Owner.Name + " to " + targetName + " " +
                                     BodyPartMessage(targetedArea) + ".",
                                     null, sourceActor.Uid);
                return true;
            }
            return false;
        }

        private string BodyPartMessage(BodyPart part)
        {
            string message = "";
            switch (part)
            {
                case BodyPart.Groin:
                    message = "nuts";
                    break;
                case BodyPart.Torso:
                    message = "chest";
                    break;
                case BodyPart.Head:
                    message = "head";
                    break;
                case BodyPart.Left_Arm:
                    message = "arm";
                    break;
                case BodyPart.Right_Arm:
                    message = "arm";
                    break;
                case BodyPart.Left_Leg:
                    message = "leg";
                    break;
                case BodyPart.Right_Leg:
                    message = "leg";
                    break;
            }
            return message;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName)
            {
                case "healAmount":
                    healAmount = parameter.GetValue<int>();
                    break;
                case "damageType":
                    if (parameter.ParameterType == typeof (string))
                    {
                        //Try to parse it. Set to Bludgeoning damagetype if parsing fails
                        if (!Enum.TryParse(parameter.GetValue<string>(), true, out damType))
                            damType = DamageType.Bludgeoning;
                    }
                    else if (parameter.ParameterType == typeof (DamageType))
                    {
                        damType = parameter.GetValue<DamageType>();
                    }
                    break;
                case "capacity":
                    capacity = parameter.GetValue<int>();
                    break;
            }
        }
    }
}