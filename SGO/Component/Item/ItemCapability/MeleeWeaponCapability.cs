using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.GO;
using ServerInterfaces;
using SS13_Shared;

namespace SGO.Component.Item.ItemCapability
{
    public class MeleeWeaponCapability : ItemCapability
    {
        public int damageAmount = 10;
        public DamageType damType = DamageType.Bludgeoning;

        public MeleeWeaponCapability()
        {
            CapabilityType = SS13_Shared.GO.ItemCapabilityType.MeleeWeapon;
            capabilityName = "MeleeCapability";
            interactsWith = InteractsWith.Actor | InteractsWith.LargeObject;
        }

        public override bool ApplyTo(Entity target, Entity sourceActor)
        {
            BodyPart targetedArea = BodyPart.Torso;

            List<ComponentReplyMessage> replies = new List<ComponentReplyMessage>();
            sourceActor.SendMessage(this, SS13_Shared.GO.ComponentMessageType.GetActorSession, replies);
            if (replies.Count > 0 && replies[0].messageType == SS13_Shared.GO.ComponentMessageType.ReturnActorSession)
            {
                IPlayerSession session = (IPlayerSession)replies[0].paramsList[0];
                targetedArea = session.TargetedArea;
            }
            else throw new NotImplementedException("Actor has no session or No actor component that returns a session"); //BEEPBOOP

            if (target.HasComponent(SS13_Shared.GO.ComponentFamily.Damageable))
            {
                target.SendMessage(this, ComponentMessageType.Damage, null, owner.Owner, damageAmount, damType, targetedArea);
                return true;
            }
            return false;
        }

        public override void SetParameter(ComponentParameter parameter)
        {
            switch (parameter.MemberName)
            {
                case "damageAmount":
                    if (parameter.ParameterType == typeof(int))
                        damageAmount = (int)parameter.Parameter;
                    if (parameter.ParameterType == typeof(string))
                        damageAmount = int.Parse((string)parameter.Parameter);
                    break;
                case "damageType":
                    if (parameter.ParameterType == typeof(string))
                    {
                        //Try to parse it. Set to Bludgeoning damagetype if parsing fails
                        if(!Enum.TryParse<DamageType>((string)parameter.Parameter, true, out damType))
                            damType = DamageType.Bludgeoning;
                    }
                    else if (parameter.ParameterType == typeof(DamageType))
                    {
                        damType = (DamageType)parameter.Parameter;
                    }
                    break;
            }
        }
    }
}
