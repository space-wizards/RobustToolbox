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
            SubscribeLocalEvent<ClientUserInterfaceComponent, ComponentShutdown>(OnUserInterfaceShutdown);
        }

        private void OnUserInterfaceShutdown(EntityUid uid, ClientUserInterfaceComponent component, ComponentShutdown args)
        {
            foreach (var bui in component.Interfaces)
            {
                bui.Dispose();
            }
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
