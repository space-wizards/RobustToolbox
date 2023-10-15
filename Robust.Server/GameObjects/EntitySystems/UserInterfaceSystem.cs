using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.GameObjects
{
    public sealed class UserInterfaceSystem : SharedUserInterfaceSystem
    {
        [Dependency] private readonly IPlayerManager _playerMan = default!;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<BoundUIWrapMessage>(OnMessageReceived);
            _playerMan.PlayerStatusChanged += OnPlayerStatusChanged;
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _playerMan.PlayerStatusChanged -= OnPlayerStatusChanged;
        }

        private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            if (args.NewStatus != SessionStatus.Disconnected)
                return;

            if (!TryComp(args.Session.AttachedEntity, out ActorUIComponent? actorUIComponent))
                return;

            foreach (var bui in actorUIComponent.OpenBUIS.ToArray())
            {
                CloseShared(bui, args.Session);
            }
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            var query = AllEntityQuery<ActiveUserInterfaceComponent, TransformComponent>();

            while (query.MoveNext(out var uid, out var activeUis, out var xform))
            {
                foreach (var ui in activeUis.Interfaces)
                {
                    CheckRange(uid, activeUis, ui, xform);

                    if (!ui.StateDirty)
                        continue;

                    ui.StateDirty = false;

                    foreach (var (player, state) in ui.PlayerStateOverrides)
                    {
                        RaiseNetworkEvent(state, player.ConnectedClient);
                    }

                    if (ui.LastStateMsg == null)
                        continue;

                    foreach (var session in ui.SubscribedSessions)
                    {
                        if (!ui.PlayerStateOverrides.ContainsKey(session))
                            RaiseNetworkEvent(ui.LastStateMsg, session.ConnectedClient);
                    }
                }
            }
        }
    }
}
