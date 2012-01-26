using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS3D_shared.GO;
using SS3D_shared;
using System.Drawing;
using ClientServices;
using ClientInterfaces;

namespace CGO
{
    public class DamageableComponent : GameObjectComponent //The basic Damageable component does not recieve health updates from the server and doesnt know what its health is.
    {                                                      //Used for things that are binary. Either broken or not broken. (windows?)
        protected bool isDead = false;

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
                    ComponentReplyMessage reply2 = new ComponentReplyMessage(ComponentMessageType.CurrentHealth, isDead ? 0 : 1, 1); //HANDLE THIS CORRECTLY
                    replies.Add(reply2);
                    break;
                default:
                    base.RecieveMessage(sender, type, replies, list);
                    break;
            }
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            ComponentMessageType type = (ComponentMessageType)message.messageParameters[0];

            switch (type)
            {
                case (ComponentMessageType.HealthStatus):
                    bool newIsDeadState = (bool)message.messageParameters[1];

                    if(newIsDeadState == true && isDead == false)
                        Owner.SendMessage(this, ComponentMessageType.Die, null);

                    isDead = newIsDeadState;
                    break;
            }
        }
    }
}
