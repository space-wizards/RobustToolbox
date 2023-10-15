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

            SubscribeLocalEvent<ActorUIComponent, AfterAutoHandleStateEvent>(OnActorUIAuto);
            SubscribeNetworkEvent<BoundUIWrapMessage>(MessageReceived);
        }

        private void OnActorUIAuto(EntityUid uid, ActorUIComponent component, ref AfterAutoHandleStateEvent args)
        {
            // TODO: Open UIs or w/e
            throw new NotImplementedException();
        }

        private void MessageReceived(BoundUIWrapMessage ev)
        {
            var uid = GetEntity(ev.Entity);

            if (!TryComp<UserInterfaceComponent>(uid, out var cmp))
                return;

            var uiKey = ev.UiKey;
            var message = ev.Message;
            // This should probably not happen at this point, but better make extra sure!
            if (_playerManager.LocalPlayer != null)
                message.Session = _playerManager.LocalPlayer.Session;

            message.Entity = GetNetEntity(uid);
            message.UiKey = uiKey;

            // Raise as object so the correct type is used.
            RaiseLocalEvent(uid, (object)message, true);

            if (cmp.OpenInterfaces.TryGetValue(uiKey, out var bui))
                bui.InternalReceiveMessage(message);
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

            var playerSession = _playerManager.LocalPlayer?.Session;
            if (playerSession != null)
            {
                uiComp.Interfaces[uiKey]._subscribedSessions.Add(playerSession);
                RaiseLocalEvent(uid, new BoundUIOpenedEvent(uiKey, uid, playerSession), true);
            }

            return true;
        }
    }
}
