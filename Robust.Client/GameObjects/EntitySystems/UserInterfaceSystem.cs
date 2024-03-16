using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using System;
using UserInterfaceComponent = Robust.Shared.GameObjects.UserInterfaceComponent;

namespace Robust.Client.GameObjects
{
    public sealed class UserInterfaceSystem : SharedUserInterfaceSystem
    {
        [Dependency] private readonly IDynamicTypeFactory _dynamicTypeFactory = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<BoundUIWrapMessage>(MessageReceived);
        }

        private void MessageReceived(BoundUIWrapMessage ev)
        {
            var uid = GetEntity(ev.Entity);

            if (!TryComp<UserInterfaceComponent>(uid, out var cmp))
                return;

            var uiKey = ev.UiKey;
            var message = ev.Message;
            message.Session = _playerManager.LocalSession!;
            message.Entity = GetNetEntity(uid);
            message.UiKey = uiKey;

            // Raise as object so the correct type is used.
            RaiseLocalEvent(uid, (object)message, true);

            switch (message)
            {
                case OpenBoundInterfaceMessage _:
                    TryOpenUi(uid, uiKey, cmp);
                    break;

                case CloseBoundInterfaceMessage _:
                    TryCloseUi(message.Session, uid, uiKey, remoteCall: true, uiComp: cmp);
                    break;

                default:
                    if (cmp.OpenInterfaces.TryGetValue(uiKey, out var bui))
                        bui.InternalReceiveMessage(message);

                    break;
            }
        }

        private bool TryOpenUi(EntityUid uid, Enum uiKey, UserInterfaceComponent? uiComp = null)
        {
            if (!Resolve(uid, ref uiComp))
                return false;

            if (uiComp.OpenInterfaces.ContainsKey(uiKey))
                return false;

            var data = uiComp.MappedInterfaceData[uiKey];

            // TODO: This type should be cached, but I'm too lazy.
            var type = _reflectionManager.LooseGetType(data.ClientType);
            var boundInterface =
                (BoundUserInterface) _dynamicTypeFactory.CreateInstance(type, new object[] {uid, uiKey});

            boundInterface.Open();
            uiComp.OpenInterfaces[uiKey] = boundInterface;

            if (_playerManager.LocalSession is { } playerSession)
            {
                uiComp.Interfaces[uiKey]._subscribedSessions.Add(playerSession);
                RaiseLocalEvent(uid, new BoundUIOpenedEvent(uiKey, uid, playerSession), true);
            }

            return true;
        }
    }
}
