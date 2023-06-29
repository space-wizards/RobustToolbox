using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using System;

namespace Robust.Client.GameObjects
{
    [UsedImplicitly]
    public sealed class UserInterfaceSystem : SharedUserInterfaceSystem
    {
        [Dependency] private readonly IDynamicTypeFactory _dynamicTypeFactory = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<BoundUIWrapMessage>(MessageReceived);
            SubscribeLocalEvent<ClientUserInterfaceComponent, ComponentInit>(OnUserInterfaceInit);
            SubscribeLocalEvent<ClientUserInterfaceComponent, ComponentShutdown>(OnUserInterfaceShutdown);
        }

        private void OnUserInterfaceInit(EntityUid uid, ClientUserInterfaceComponent component, ComponentInit args)
        {
            component._interfaces.Clear();

            foreach (var data in component._interfaceData)
            {
                component._interfaces[data.UiKey] = data;
            }
        }

        private void OnUserInterfaceShutdown(EntityUid uid, ClientUserInterfaceComponent component, ComponentShutdown args)
        {
            foreach (var bui in component.OpenInterfaces.Values)
            {
                bui.Dispose();
            }
        }

        private void MessageReceived(BoundUIWrapMessage ev)
        {
            var uid = ev.Entity;
            if (!TryComp<ClientUserInterfaceComponent>(uid, out var cmp))
                return;

            var uiKey = ev.UiKey;
            var message = ev.Message;
            // This should probably not happen at this point, but better make extra sure!
            if(_playerManager.LocalPlayer != null)
                message.Session = _playerManager.LocalPlayer.Session;

            message.Entity = uid;
            message.UiKey = uiKey;

            // Raise as object so the correct type is used.
            RaiseLocalEvent(uid, (object)message, true);

            switch (message)
            {
                case OpenBoundInterfaceMessage _:
                    TryOpenUi(uid, uiKey, cmp);
                    break;

                case CloseBoundInterfaceMessage _:
                    TryCloseUi(uid, uiKey, remoteCall: true, uiComp: cmp);
                    break;

                default:
                    if (cmp.OpenInterfaces.TryGetValue(uiKey, out var bui))
                        bui.InternalReceiveMessage(message);

                    break;
            }
        }

        private bool TryOpenUi(EntityUid uid, Enum uiKey, ClientUserInterfaceComponent? uiComp = null)
        {
            if (!Resolve(uid, ref uiComp))
                return false;

            if (uiComp.OpenInterfaces.ContainsKey(uiKey))
                return false;

            var data = uiComp._interfaces[uiKey];

            // TODO: This type should be cached, but I'm too lazy.
            var type = _reflectionManager.LooseGetType(data.ClientType);
            var boundInterface =
                (BoundUserInterface) _dynamicTypeFactory.CreateInstance(type, new object[] {uid, uiKey});

            boundInterface.Open();
            uiComp.OpenInterfaces[uiKey] = boundInterface;

            var playerSession = _playerManager.LocalPlayer?.Session;
            if(playerSession != null)
                RaiseLocalEvent(uid, new BoundUIOpenedEvent(uiKey, uid, playerSession), true);

            return true;
        }

        internal bool TryCloseUi(EntityUid uid, Enum uiKey, bool remoteCall = false, ClientUserInterfaceComponent? uiComp = null)
        {
            if (!Resolve(uid, ref uiComp))
                return false;

            if (!uiComp.OpenInterfaces.TryGetValue(uiKey, out var boundUserInterface))
                return false;

            if (!remoteCall)
                SendUiMessage(boundUserInterface, new CloseBoundInterfaceMessage());

            uiComp.OpenInterfaces.Remove(uiKey);
            boundUserInterface.Dispose();

            var playerSession = _playerManager.LocalPlayer?.Session;
            if(playerSession != null)
                RaiseLocalEvent(uid, new BoundUIClosedEvent(uiKey, uid, playerSession), true);

            return true;
        }

        internal void SendUiMessage(BoundUserInterface bui, BoundUserInterfaceMessage msg)
        {
            RaiseNetworkEvent(new BoundUIWrapMessage(bui.Owner, msg, bui.UiKey));
        }
    }
}
