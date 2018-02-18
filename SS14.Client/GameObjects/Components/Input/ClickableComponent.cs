using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using System;
using SS14.Shared.Enums;
using SS14.Shared.Input;
using SS14.Shared.Map;

namespace SS14.Client.GameObjects
{
    // Notice: Most actual logic for clicking is done by the game screen.
    public class ClickableComponent : Component, IClientClickableComponent
    {
        public override string Name => "Clickable";
        public override uint? NetID => NetIDs.CLICKABLE;

        [Obsolete("Subscribe to ClientClick message.")]
        public event EventHandler<ClickEventArgs> OnClick;

        public bool CheckClick(LocalCoordinates worldPos, out int drawdepth)
        {
            var component = Owner.GetComponent<IClickTargetComponent>();

            drawdepth = (int)component.DrawDepth;
            return component.WasClicked(worldPos);
        }

        public void DispatchClick(IEntity user, ClickType clickType)
        {
            OnClick?.Invoke(this, new ClickEventArgs(user, Owner, clickType));

            var message = new ClientClickMsg(user.Uid, clickType);
            SendMessage(message);
            SendNetworkMessage(message);
        }
    }
}
