using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.GOC;
using ClientInterfaces.UserInterface;
using GorgonLibrary;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using GorgonLibrary.Graphics;
using SS13.IoC;

namespace CGO
{
    public class PlayerAction : IPlayerAction
    {
        protected readonly uint uid = 0;
        public uint Uid { get { return uid; } }

        protected PlayerActionTargetType targetType = PlayerActionTargetType.Any;
        public PlayerActionTargetType TargetType { get { return targetType; } }

        protected String name = "Empty Action";
        public String Name { get { return name; } }

        protected String description = "This is an undefined Action.";
        public String Description { get { return description; } }

        protected String icon = "action_none";
        public String Icon { get { return icon; } }

        public DateTime cooldownExpires;
        public DateTime CooldownExpires { get { return cooldownExpires; } }

        private PlayerActionComp parent;

        public PlayerAction(uint _uid, PlayerActionComp _parent) //Do not add more parameters to the constructors or bad things happen.
        {
            uid = _uid;
            parent = _parent;
        }

        public void Activate() //Activates this action. If it's targeted, the player will enter targeting mode - else it will be used.
        {
            if (cooldownExpires.Subtract(DateTime.Now).TotalSeconds > 0) return;

            if (targetType == SS13_Shared.PlayerActionTargetType.None)
                Use(null);
            else
                IoCManager.Resolve<IUserInterfaceManager>().StartTargeting(this);
        }

        public void Use(object target)
        {
            parent.SendDoAction(this, target);
        }
    }
}
