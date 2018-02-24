using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using System;

namespace SS14.Server.GameObjects
{
    public class ClickableComponent : Component, IClickableComponent
    {
        public override string Name => "Clickable";
        public override uint? NetID => NetIDs.CLICKABLE;

        [Obsolete("Subscribe to ClientClick message.")]
        public event EventHandler<ClickEventArgs> OnClick;

        public override void HandleMessage(object owner, ComponentMessage message)
        {
            base.HandleMessage(owner, message);

            switch (message)
            {
                case ClientClickMsg msg:
                    var type = msg.Click;
                    var uid = msg.Uid;
                    var user = Owner.EntityManager.GetEntity(uid);

                    OnClick?.Invoke(this, new ClickEventArgs(user, Owner, type));
                    Owner.RaiseEvent(new ClickedOnEntityEventArgs { Clicked = Owner.Uid, Clicker = uid, MouseButton = type });
                    break;
            }
        }
    }
}
