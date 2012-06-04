using System;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces;
using ServerInterfaces.Chat;
using ServerInterfaces.Player;

namespace SGO.Component.Item.ItemCapability
{
    public class MeleeWeaponCapability : ItemCapability
    {
        public DamageType damType = DamageType.Bludgeoning;
        public int damageAmount = 10;
        public bool toggleable = false;
        public bool active = true;
        public string activeSprite;
        public string inactiveSprite;

        public MeleeWeaponCapability()
        {
            CapabilityType = ItemCapabilityType.MeleeWeapon;
            capabilityName = "MeleeCapability";
            interactsWith = InteractsWith.Actor | InteractsWith.LargeObject;
        }

        public override bool ApplyTo(Entity target, Entity sourceActor)
        {
            string sourceName = sourceActor.Name;
            string targetName = (sourceActor.Uid == target.Uid) ? "himself" : target.Name;
            if (!active)
            {
                IoCManager.Resolve<IChatManager>()
                    .SendChatMessage(ChatChannel.Damage, sourceName + " tries to attack " + targetName
                                                         + " with the " + owner.Owner.Name + ", but nothing happens!",
                                     null, sourceActor.Uid);
                return true;
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

            //Damage the item that is doing the damage.
            owner.Owner.SendMessage(this, ComponentMessageType.Damage, owner.Owner, 5, DamageType.Collateral);

            //Damage the target.
            if (target.HasComponent(ComponentFamily.Damageable))
            {
                target.SendMessage(this, ComponentMessageType.Damage, owner.Owner, damageAmount, damType, targetedArea);


                //string suffix = (sourceActor.Uid == target.Uid) ? " What a fucking weirdo..." : "";
                IoCManager.Resolve<IChatManager>()
                    .SendChatMessage(ChatChannel.Damage,
                                     sourceName + " " + DamTypeMessage(damType) + " " + targetName + " in the " +
                                     BodyPartMessage(targetedArea) + " with a " + owner.Owner.Name + "!",
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

        private string DamTypeMessage(DamageType type)
        {
            string message = "";
            switch (type)
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
                    if (parameter.ParameterType == typeof (int))
                        damageAmount = (int) parameter.Parameter;
                    if (parameter.ParameterType == typeof (string))
                        damageAmount = int.Parse((string) parameter.Parameter);
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
                case "startActive":
                    if (parameter.ParameterType == typeof(bool))
                        active = (bool)parameter.Parameter;
                    break;
                case "toggleable":
                    if (parameter.ParameterType == typeof(bool))
                        toggleable = (bool)parameter.Parameter;
                    if (!toggleable)
                        active = true;
                    break;
                case "inactiveSprite":
                    if (parameter.ParameterType == typeof(string))
                        inactiveSprite = (string) parameter.Parameter;
                    break;
                case "activeSprite":
                    if (parameter.ParameterType == typeof(string))
                        activeSprite = (string)parameter.Parameter;
                    break;
            }
        }

        public override void Activate()
        {
            base.Activate();

            if (!toggleable)
                return;

            if (!active && activeSprite != null)
                owner.Owner.SendMessage(this, ComponentMessageType.SetBaseName, activeSprite);
            if (active && inactiveSprite != null)
                owner.Owner.SendMessage(this, ComponentMessageType.SetBaseName, inactiveSprite);

            active = !active;
        }
    }
}