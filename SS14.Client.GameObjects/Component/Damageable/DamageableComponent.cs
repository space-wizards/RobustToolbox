using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Damageable;
using System;

namespace SS14.Client.GameObjects
{
    public class DamageableComponent : Component
        //The basic Damageable component does not recieve health updates from the server and doesnt know what its health is.
    {
        //Used for things that are binary. Either broken or not broken. (windows?)
        protected bool IsDead;

        public DamageableComponent()
        {
            Family = ComponentFamily.Damageable;
            ;
        }

        public override Type StateType
        {
            get { return typeof (DamageableComponentState); }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.GetCurrentHealth:
                    reply = new ComponentReplyMessage(ComponentMessageType.CurrentHealth, IsDead ? 0 : 1, 1);
                    //HANDLE THIS CORRECTLY
                    break;
            }

            return reply;
        }

        protected virtual void Die()
        {
            if (IsDead) return;

            IsDead = true;
            Owner.SendMessage(this, ComponentMessageType.Die);
        }

        public override void HandleComponentState(dynamic state)
        {
            dynamic newIsDeadState = state.IsDead;

            if (newIsDeadState && IsDead == false)
                Die();
        }
    }
}