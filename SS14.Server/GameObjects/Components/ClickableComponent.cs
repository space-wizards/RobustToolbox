using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using System;

namespace SS14.Server.GameObjects
{
    public class ClickableComponent : Component, IClickableComponent
    {
        public override string Name => "Clickable";
        public override uint? NetID => NetIDs.CLICKABLE;

        public event EventHandler<ClickEventArgs> OnClick;

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            var type = (int)message.MessageParameters[0]; // Click type.
            var uid = (int)message.MessageParameters[1]; // ID of the user
            var user = Owner.EntityManager.GetEntity(uid);

            OnClick?.Invoke(this, new ClickEventArgs(user, Owner, type));
            Owner.RaiseEvent(new ClickedOnEntityEventArgs { Clicked = Owner.Uid, Clicker = uid, MouseButton = type });
        }
    }
}
