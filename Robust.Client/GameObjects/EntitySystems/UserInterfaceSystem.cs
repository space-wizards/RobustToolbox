using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using System;
using Robust.Shared.GameStates;
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

            SubscribeLocalEvent<ActorUIComponent, ComponentHandleState>(OnActorUiHandleState);
            SubscribeNetworkEvent<BoundUIWrapMessage>(MessageReceived);
        }

        private void OnActorUiHandleState(EntityUid uid, ActorUIComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not ActorUIComponentState state)
                return;

            TryComp<ActorComponent>(uid, out var actorComp);
            var session = actorComp?.Session;

            foreach (var bui in component.OpenBUIS)
            {
                if (!TryGetEntity(bui.Owner, out var buiEntity))
                    continue;

                TryClose(session, buiEntity.Value, bui.UiKey);
            }

            foreach (var bui in state.OpenBUIS)
            {
                if (!TryGetEntity(bui.Owner, out var buiEntity))
                    continue;

                ClientOpenUi(buiEntity.Value, bui.UiKey);
            }

            component.OpenBUIS.Clear();
            component.OpenBUIS.AddRange(state.OpenBUIS);
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

        protected override void OpenUiLocal(NetEntity netEntity, Enum uiKey)
        {
            base.OpenUiLocal(netEntity, uiKey);
            RaisePredictiveEvent(new BoundUIWrapMessage(netEntity, new OpenBoundInterfaceMessage(), uiKey));
            ClientOpenUi(GetEntity(netEntity), uiKey);
        }

        /// <summary>
        /// Opens a UI on the client. Required to instantiate the BUI class.
        /// </summary>
        private void ClientOpenUi(EntityUid uid, Enum uiKey, UserInterfaceComponent? uiComp = null)
        {
            if (!Resolve(uid, ref uiComp)) return;

            if (uiComp.OpenInterfaces.ContainsKey(uiKey)) return;

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
        }
    }
}
