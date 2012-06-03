using System;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.Chat;
using ServerInterfaces.Player;

namespace SGO.Component.Item.ItemCapability
{
    public class MedicalCapability : ItemCapability
    {
        public DamageType damType = DamageType.Bludgeoning;
        public int healAmount = 10;
        public int capacity = 100; //Healing capacity

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
            BodyPart targetedArea = BodyPart.Torso;

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
                                     sourceName + " applies the " + owner.Owner.Name + " to "  + targetName + " " +
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
                    if (parameter.ParameterType == typeof (int))
                        healAmount = (int) parameter.Parameter;
                    if (parameter.ParameterType == typeof (string))
                        healAmount = int.Parse((string) parameter.Parameter);
                    break;
                case "damageType":
                    if (parameter.ParameterType == typeof (string))
                    {
                        //Try to parse it. Set to Bludgeoning damagetype if parsing fails
                        if (!Enum.TryParse((string) parameter.Parameter, true, out damType))
                            damType = DamageType.Bludgeoning;
                    }
                    else if (parameter.ParameterType == typeof (DamageType))
                    {
                        damType = (DamageType) parameter.Parameter;
                    }
                    break;
                case "capacity":
                    if (parameter.ParameterType == typeof(int))
                        capacity = (int) parameter.Parameter;
                    if (parameter.ParameterType == typeof(string))
                        capacity = int.Parse((string) parameter.Parameter);
                    break;

            }
        }
    }
}