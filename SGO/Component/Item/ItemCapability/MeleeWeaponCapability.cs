using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared.GO;
using ServerInterfaces;
using SS13_Shared;
using ServerServices;

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
            var targetedArea = BodyPart.Torso;

            var reply = sourceActor.SendMessage(this, ComponentFamily.Actor, SS13_Shared.GO.ComponentMessageType.GetActorSession);
            if (reply.MessageType == SS13_Shared.GO.ComponentMessageType.ReturnActorSession)
            {
                IPlayerSession session = (IPlayerSession)reply.ParamsList[0];
                targetedArea = session.TargetedArea;
            }
            else throw new NotImplementedException("Actor has no session or No actor component that returns a session"); //BEEPBOOP

            if (target.HasComponent(SS13_Shared.GO.ComponentFamily.Damageable))
            {
                target.SendMessage(this, ComponentMessageType.Damage, owner.Owner, damageAmount, damType, targetedArea);

                var sourceName = sourceActor.Name;
                var targetName = (sourceActor.Uid == target.Uid) ? "himself" : target.Name;
                var suffix = (sourceActor.Uid == target.Uid) ? " What a fucking weirdo..." : "";
                ServiceManager.Singleton.Resolve<IChatManager>()
                    .SendChatMessage(ChatChannel.Damage, 
                    sourceName + " " + DamTypeMessage(damType) + " " + targetName + " in the " + BodyPartMessage(targetedArea) + " with a " + owner.Owner.Name + "!" + suffix, 
                    null, sourceActor.Uid);
                return true;
            }
            return false;
        }

        private string BodyPartMessage(BodyPart part)
        {
            var message = "";
            switch(part)
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

        private string DamTypeMessage(DamageType type)
        {
            var message = "";
            switch(type)
            {
                case DamageType.Slashing:
                    message = "slashes";
                    break;
                case DamageType.Toxin:
                    break;
                case DamageType.Piercing:
                    message = "stabs";
                    break;
                case DamageType.Bludgeoning:
                    message = "clobbers";
                    break;
                case DamageType.Freeze:
                    break;
                case DamageType.Untyped:
                    break;
                case DamageType.Suffocation:
                    break;
                case DamageType.Burn:
                    message = "burns";
                    break;
            }
            return message;
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
