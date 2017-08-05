using Lidgren.Network;
using SFML.System;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using System;

namespace SS14.Client.GameObjects
{
    // Notice: Most actual logic for clicking is done by the game screen.
    public class ClickableComponent : ClientComponent, IClientClickableComponent
    {
        public override string Name => "Clickable";
        public override uint? NetID => NetIDs.CLICKABLE;

        public event EventHandler<ClickEventArgs> OnClick;

        public bool CheckClick(Vector2f worldPos, out int drawdepth)
        {
            var component = Owner.GetComponent<IClickTargetComponent>();

            drawdepth = (int)component.DrawDepth;
            return component.WasClicked(worldPos);
        }

        public void DispatchClick(IEntity user, int clickType)
        {
            OnClick?.Invoke(this, new ClickEventArgs(user, Owner, clickType));

            Owner.SendComponentNetworkMessage(this,
                                              NetDeliveryMethod.ReliableOrdered,
                                              clickType,
                                              user.Uid);
        }
    }
}
