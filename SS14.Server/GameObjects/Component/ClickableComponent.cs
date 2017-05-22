using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;

namespace SS14.Server.GameObjects
{
    public class ClickableComponent : Component
    {
        public ClickableComponent()
        {
            Family = ComponentFamily.Click;
        }

        /// <summary>
        /// NetMessage handler
        /// </summary>
        /// <param name="message"></param>
        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            if (message.ComponentFamily == ComponentFamily.Click)
            {
                var type = (ComponentMessageType) message.MessageParameters[0];
                var uid = (int) message.MessageParameters[1];
                var mouseClickType = MouseClickType.None;
                if(type == ComponentMessageType.LeftClick || type == ComponentMessageType.RightClick || type == ComponentMessageType.ClickedInHand)
                {
                    Owner.SendMessage(this, type, uid);
                    mouseClickType = MouseClickType.ConvertComponentMessageTypeToClickType(type);
                }
                Owner.RaiseEvent(new ClickedOnEntityEventArgs { Clicked = Owner.Uid, Clicker = uid, MouseButton = mouseClickType });
            }
        }
    }
}
