using System;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;  
using SS14.Shared.Reflection;

namespace SS14.Client.GameObjects
{
    /// <summary>
    /// Sub type of <see cref="Component"/> that handles some client-specific things.
    /// </summary>
    [Reflect(false)]
    public abstract class ClientComponent : Component
    {
        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                    params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return reply;

            if (type == ComponentMessageType.Initialize)
            {
                SendComponentInstantiationMessage();
            }

            return reply;
        }

        public override void OnAdd(IEntity owner)
        {
            base.OnAdd(owner);
            if (Owner.Initialized)
            {
                SendComponentInstantiationMessage();
            }
        }

        /// <summary>
        /// Client message to server saying component has been instantiated and needs initial data
        /// </summary>
        [Obsolete("Getting rid of this messaging paradigm.")]
        public void SendComponentInstantiationMessage()
        {
            var manager = IoCManager.Resolve<IEntityNetworkManager>();
            manager.SendEntityNetworkMessage(
                Owner,
                EntityMessage.ComponentInstantiationMessage,
                Family);
        }
    }
}
