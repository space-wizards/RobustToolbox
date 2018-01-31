using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using System;

namespace SS14.Server.GameObjects
{
    public class ClickableComponent : Component, IClickableComponent
    {
        public override string Name => "Clickable";
        public override uint? NetID => NetIDs.CLICKABLE;

        public event EventHandler<ClickEventArgs> OnClick;

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            var type = (int)message.MessageParameters[0]; // Click type.
            var uid = (int)message.MessageParameters[1]; // ID of the user
            var user = Owner.EntityManager.GetEntity(new EntityUid(uid));

            OnClick?.Invoke(this, new ClickEventArgs(user, Owner, type));
            Owner.RaiseEvent(new ClickedOnEntityEventArgs { Clicked = Owner.Uid, Clicker = new EntityUid(uid), MouseButton = type });
        }
    }
}
