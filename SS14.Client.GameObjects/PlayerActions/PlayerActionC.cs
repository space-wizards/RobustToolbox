using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared;

using System;

namespace SS14.Client.GameObjects
{
    public class PlayerAction : IPlayerAction
    {
        private readonly PlayerActionComp parent;
        protected readonly uint uid = 0;
        protected DateTime cooldownExpires;
        protected String description = "This is an undefined Action.";
        protected String icon = "action_none";
        protected String name = "Empty Action";

        protected PlayerActionTargetType targetType = PlayerActionTargetType.Any;

        public PlayerAction(uint _uid, PlayerActionComp _parent)
            //Do not add more parameters to the constructors or bad things happen.
        {
            uid = _uid;
            parent = _parent;
        }

        #region IPlayerAction Members

        public uint Uid
        {
            get { return uid; }
        }

        public PlayerActionTargetType TargetType
        {
            get { return targetType; }
        }

        public String Name
        {
            get { return name; }
        }

        public String Description
        {
            get { return description; }
        }

        public String Icon
        {
            get { return icon; }
        }

        public DateTime CooldownExpires
        {
            get { return cooldownExpires; }
            set { cooldownExpires = value; }
        }

        public void Activate()
            //Activates this action. If it's targeted, the player will enter targeting mode - else it will be used.
        {
            if (cooldownExpires.Subtract(DateTime.Now).TotalSeconds > 0) return;

            if (targetType == PlayerActionTargetType.None)
                Use(null);
            else
                IoCManager.Resolve<IUserInterfaceManager>().StartTargeting(this);
        }

        public void Use(object target)
        {
            parent.SendDoAction(this, target);
        }

        #endregion
    }
}