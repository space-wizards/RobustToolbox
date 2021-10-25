using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.GameObjects
{
    [UsedImplicitly]
    public sealed class UserInterfaceSystem : SharedUserInterfaceSystem
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;

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
            var uid = ev.Entity;
            if (!EntityManager.TryGetComponent<ClientUserInterfaceComponent>(uid, out var cmp))
                return;

            var message = ev.Message;
            // This should probably not happen at this point, but better make extra sure!
            if(_playerManager.LocalPlayer != null)
                message.Session = _playerManager.LocalPlayer.Session;
            message.Entity = uid;
            message.UiKey = ev.UiKey;

            // Raise as object so the correct type is used.
            RaiseLocalEvent(uid, (object)message);

            cmp.MessageReceived(ev);
        }

        internal void Send(BoundUIWrapMessage msg)
        {
            RaiseNetworkEvent(msg);
        }
    }
}
