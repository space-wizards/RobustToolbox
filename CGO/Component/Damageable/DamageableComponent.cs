using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GO.Component.Damageable;

namespace CGO
{
    public class DamageableComponent : GameObjectComponent //The basic Damageable component does not recieve health updates from the server and doesnt know what its health is.
    {                                                      //Used for things that are binary. Either broken or not broken. (windows?)
        protected bool IsDead;

        public DamageableComponent() :base()
        {
            Family = ComponentFamily.Damageable; ;
        }

        public override System.Type StateType
        {
            get { return typeof (DamageableComponentState); }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;
            
            switch (type)
            {
                case ComponentMessageType.GetCurrentHealth:
                    reply = new ComponentReplyMessage(ComponentMessageType.CurrentHealth, IsDead ? 0 : 1, 1); //HANDLE THIS CORRECTLY
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
            var newIsDeadState = state.IsDead;

            if(newIsDeadState && IsDead == false)
                Die();
        }
    }
}
