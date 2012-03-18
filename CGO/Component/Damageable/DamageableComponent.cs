using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class DamageableComponent : GameObjectComponent //The basic Damageable component does not recieve health updates from the server and doesnt know what its health is.
    {                                                      //Used for things that are binary. Either broken or not broken. (windows?)
        protected bool IsDead;

        public override ComponentFamily Family
        {
            get { return ComponentFamily.Damageable; }
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

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            var type = (ComponentMessageType)message.MessageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.HealthStatus):
                    var newIsDeadState = (bool)message.MessageParameters[1];

                    if(newIsDeadState == true && IsDead == false)
                        Owner.SendMessage(this, ComponentMessageType.Die);

                    IsDead = newIsDeadState;
                    break;
            }
        }
    }
}
