using System.Collections.Generic;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO.Component.Damageable
{
    public class DamageableComponent : GameObjectComponent //The basic Damageable component does not recieve health updates from the server and doesnt know what its health is.
    {                                                      //Used for things that are binary. Either broken or not broken. (windows?)
        protected bool IsDead;

        public DamageableComponent()
            : base()
        {
            family = ComponentFamily.Damageable;
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.GetCurrentHealth:
                    var reply2 = new ComponentReplyMessage(ComponentMessageType.CurrentHealth, IsDead ? 0 : 1, 1); //HANDLE THIS CORRECTLY
                    replies.Add(reply2);
                    break;
                default:
                    base.RecieveMessage(sender, type, replies, list);
                    break;
            }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            var type = (ComponentMessageType)message.MessageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.HealthStatus):
                    var newIsDeadState = (bool)message.MessageParameters[1];

                    if(newIsDeadState == true && IsDead == false)
                        Owner.SendMessage(this, ComponentMessageType.Die, null);

                    IsDead = newIsDeadState;
                    break;
            }
        }
    }
}
