using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects
{
    [UsedImplicitly]
    public sealed class UserInterfaceSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<BoundUIWrapMessage>(MessageReceived);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeNetworkEvent<BoundUIWrapMessage>();
        }

        private void MessageReceived(BoundUIWrapMessage ev)
        {
            var cmp = ComponentManager.GetComponent<ClientUserInterfaceComponent>(ev.Entity);

            cmp.MessageReceived(ev);
        }

        internal void Send(BoundUIWrapMessage msg)
        {
            RaiseNetworkEvent(msg);
        }
    }
}
