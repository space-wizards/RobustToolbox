using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Robust.Server.GameObjects
{
    public sealed class UserInterfaceSystem : SharedUserInterfaceSystem
    {
        [Dependency] private readonly IPlayerManager _playerMan = default!;
        [Dependency] private readonly TransformSystem _xformSys = default!;

        private EntityQuery<IgnoreUIRangeComponent> _ignoreUIRangeQuery;

        private readonly List<ICommonSession> _sessionCache = new();

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<BoundUIWrapMessage>(OnMessageReceived);
            _playerMan.PlayerStatusChanged += OnPlayerStatusChanged;

            _ignoreUIRangeQuery = GetEntityQuery<IgnoreUIRangeComponent>();
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

            if (!OpenInterfaces.TryGetValue(args.Session, out var buis))
                return;

            foreach (var bui in buis.ToArray())
            {
                CloseShared(bui, args.Session);
            }
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            var query = AllEntityQuery<ActiveUserInterfaceComponent, TransformComponent>();

            while (query.MoveNext(out var uid, out var activeUis, out var xform))
            {
                foreach (var ui in activeUis.Interfaces)
                {
                    CheckRange(uid, activeUis, ui, xform, xformQuery);

                    if (!ui.StateDirty)
                        continue;

                    ui.StateDirty = false;

                    foreach (var (player, state) in ui.PlayerStateOverrides)
                    {
                        RaiseNetworkEvent(state, player.Channel);
                    }

                    if (ui.LastStateMsg == null)
                        continue;

                    foreach (var session in ui.SubscribedSessions)
                    {
                        if (!ui.PlayerStateOverrides.ContainsKey(session))
                            RaiseNetworkEvent(ui.LastStateMsg, session.Channel);
                    }
                }
            }
        }

        /// <summary>
        ///     Verify that the subscribed clients are still in range of the interface.
        /// </summary>
        private void CheckRange(EntityUid uid, ActiveUserInterfaceComponent activeUis, PlayerBoundUserInterface ui, TransformComponent transform, EntityQuery<TransformComponent> query)
        {
            if (ui.InteractionRange <= 0)
                return;

            // We have to cache the set of sessions because Unsubscribe modifies the original.
            _sessionCache.Clear();
            _sessionCache.AddRange(ui.SubscribedSessions);

            var uiPos = _xformSys.GetWorldPosition(transform, query);
            var uiMap = transform.MapID;

            foreach (var session in _sessionCache)
            {
                // The component manages the set of sessions, so this invalid session should be removed soon.
                if (!query.TryGetComponent(session.AttachedEntity, out var xform))
                    continue;

                if (_ignoreUIRangeQuery.HasComponent(session.AttachedEntity))
                    continue;

                // Handle pluggable BoundUserInterfaceCheckRangeEvent
                var checkRangeEvent = new BoundUserInterfaceCheckRangeEvent(uid, ui, session);
                RaiseLocalEvent(uid, ref checkRangeEvent, broadcast: true);
                if (checkRangeEvent.Result == BoundUserInterfaceRangeResult.Pass)
                    continue;

                if (checkRangeEvent.Result == BoundUserInterfaceRangeResult.Fail)
                {
                    CloseUi(ui, session, activeUis);
                    continue;
                }

                DebugTools.Assert(checkRangeEvent.Result == BoundUserInterfaceRangeResult.Default);

                if (uiMap != xform.MapID)
                {
                    CloseUi(ui, session, activeUis);
                    continue;
                }

                var distanceSquared = (uiPos - _xformSys.GetWorldPosition(xform, query)).LengthSquared();
                if (distanceSquared > ui.InteractionRangeSqrd)
                    CloseUi(ui, session, activeUis);
            }
        }

        #region Get BUI

        public bool HasUi(EntityUid uid, Enum uiKey, UserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                return false;

            return ui.Interfaces.ContainsKey(uiKey);
        }

        public PlayerBoundUserInterface GetUi(EntityUid uid, Enum uiKey, UserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                throw new InvalidOperationException($"Cannot get {typeof(PlayerBoundUserInterface)} from an entity without {typeof(UserInterfaceComponent)}!");

            return ui.Interfaces[uiKey];
        }

        public PlayerBoundUserInterface? GetUiOrNull(EntityUid uid, Enum uiKey, UserInterfaceComponent? ui = null)
        {
            return TryGetUi(uid, uiKey, out var bui, ui)
                ? bui
                : null;
        }

        /// <summary>
        ///     Return UIs a session has open.
        ///     Null if empty.
        /// </summary>
        public List<PlayerBoundUserInterface>? GetAllUIsForSession(ICommonSession session)
        {
            OpenInterfaces.TryGetValue(session, out var value);
            return value;
        }
        #endregion

        public bool IsUiOpen(EntityUid uid, Enum uiKey, UserInterfaceComponent? ui = null)
        {
            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            return bui.SubscribedSessions.Count > 0;
        }

        public bool SessionHasOpenUi(EntityUid uid, Enum uiKey, ICommonSession session, UserInterfaceComponent? ui = null)
        {
            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            return bui.SubscribedSessions.Contains(session);
        }

        /// <summary>
        ///     Sets a state. This can be used for stateful UI updating.
        ///     This state is sent to all clients, and automatically sent to all new clients when they open the UI.
        ///     Pretty much how NanoUI did it back in ye olde BYOND.
        /// </summary>
        /// <param name="state">
        ///     The state object that will be sent to all current and future client.
        ///     This can be null.
        /// </param>
        /// <param name="session">
        ///     The player session to send this new state to.
        ///     Set to null for sending it to every subscribed player session.
        /// </param>
        public bool TrySetUiState(EntityUid uid,
            Enum uiKey,
            BoundUserInterfaceState state,
            ICommonSession? session = null,
            UserInterfaceComponent? ui = null,
            bool clearOverrides = true)
        {
            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            SetUiState(bui, state, session, clearOverrides);
            return true;
        }

        /// <summary>
        ///     Sets a state. This can be used for stateful UI updating.
        ///     This state is sent to all clients, and automatically sent to all new clients when they open the UI.
        ///     Pretty much how NanoUI did it back in ye olde BYOND.
        /// </summary>
        /// <param name="state">
        ///     The state object that will be sent to all current and future client.
        ///     This can be null.
        /// </param>
        /// <param name="session">
        ///     The player session to send this new state to.
        ///     Set to null for sending it to every subscribed player session.
        /// </param>
        public void SetUiState(PlayerBoundUserInterface bui, BoundUserInterfaceState state, ICommonSession? session = null, bool clearOverrides = true)
        {
            var msg = new BoundUIWrapMessage(GetNetEntity(bui.Owner), new UpdateBoundStateMessage(state), bui.UiKey);
            if (session == null)
            {
                bui.LastStateMsg = msg;
                if (clearOverrides)
                    bui.PlayerStateOverrides.Clear();
            }
            else
            {
                bui.PlayerStateOverrides[session] = msg;
            }

            bui.StateDirty = true;
        }

        #region Close
        protected override void CloseShared(PlayerBoundUserInterface bui, ICommonSession session, ActiveUserInterfaceComponent? activeUis = null)
        {
            var owner = bui.Owner;
            bui._subscribedSessions.Remove(session);
            bui.PlayerStateOverrides.Remove(session);

            if (OpenInterfaces.TryGetValue(session, out var buis))
                buis.Remove(bui);

            RaiseLocalEvent(owner, new BoundUIClosedEvent(bui.UiKey, owner, session));

            if (bui._subscribedSessions.Count == 0)
                DeactivateInterface(bui.Owner, bui, activeUis);
        }

        /// <summary>
        ///     Closes this all interface for any clients that have any open.
        /// </summary>
        public bool TryCloseAll(EntityUid uid, Shared.GameObjects.ActiveUserInterfaceComponent? aui = null)
        {
            if (!Resolve(uid, ref aui, false))
                return false;

            foreach (var ui in aui.Interfaces)
            {
                CloseAll(ui);
            }

            return true;
        }

        /// <summary>
        ///     Closes this specific interface for any clients that have it open.
        /// </summary>
        public bool TryCloseAll(EntityUid uid, Enum uiKey, UserInterfaceComponent? ui = null)
        {
            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            CloseAll(bui);
            return true;
        }

        /// <summary>
        ///     Closes this interface for any clients that have it open.
        /// </summary>
        public void CloseAll(PlayerBoundUserInterface bui)
        {
            foreach (var session in bui.SubscribedSessions.ToArray())
            {
                CloseUi(bui, session);
            }
        }

        #endregion

        #region SendMessage

        /// <summary>
        ///     Send a BUI message to all connected player sessions.
        /// </summary>
        public bool TrySendUiMessage(EntityUid uid, Enum uiKey, BoundUserInterfaceMessage message, UserInterfaceComponent? ui = null)
        {
            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            SendUiMessage(bui, message);
            return true;
        }

        /// <summary>
        ///     Send a BUI message to all connected player sessions.
        /// </summary>
        public void SendUiMessage(PlayerBoundUserInterface bui, BoundUserInterfaceMessage message)
        {
            var msg = new BoundUIWrapMessage(GetNetEntity(bui.Owner), message, bui.UiKey);
            foreach (var session in bui.SubscribedSessions)
            {
                RaiseNetworkEvent(msg, session.Channel);
            }
        }

        /// <summary>
        ///     Send a BUI message to a specific player session.
        /// </summary>
        public bool TrySendUiMessage(EntityUid uid, Enum uiKey, BoundUserInterfaceMessage message, ICommonSession session, UserInterfaceComponent? ui = null)
        {
            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            return TrySendUiMessage(bui, message, session);
        }

        /// <summary>
        ///     Send a BUI message to a specific player session.
        /// </summary>
        public bool TrySendUiMessage(PlayerBoundUserInterface bui, BoundUserInterfaceMessage message, ICommonSession session)
        {
            if (!bui.SubscribedSessions.Contains(session))
                return false;

            RaiseNetworkEvent(new BoundUIWrapMessage(GetNetEntity(bui.Owner), message, bui.UiKey), session.Channel);
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Raised by <see cref="UserInterfaceSystem"/> to check whether an interface is still accessible by its user.
    /// </summary>
    [ByRefEvent]
    [PublicAPI]
    public struct BoundUserInterfaceCheckRangeEvent
    {
        /// <summary>
        /// The entity owning the UI being checked for.
        /// </summary>
        public readonly EntityUid Target;

        /// <summary>
        /// The UI itself.
        /// </summary>
        /// <returns></returns>
        public readonly PlayerBoundUserInterface UserInterface;

        /// <summary>
        /// The player for which the UI is being checked.
        /// </summary>
        public readonly ICommonSession Player;

        /// <summary>
        /// The result of the range check.
        /// </summary>
        public BoundUserInterfaceRangeResult Result;

        public BoundUserInterfaceCheckRangeEvent(
            EntityUid target,
            PlayerBoundUserInterface userInterface,
            ICommonSession player)
        {
            Target = target;
            UserInterface = userInterface;
            Player = player;
        }
    }

    /// <summary>
    /// Possible results for a <see cref="BoundUserInterfaceCheckRangeEvent"/>.
    /// </summary>
    public enum BoundUserInterfaceRangeResult : byte
    {
        /// <summary>
        /// Run built-in range check.
        /// </summary>
        Default,

        /// <summary>
        /// Range check passed, UI is accessible.
        /// </summary>
        Pass,

        /// <summary>
        /// Range check failed, UI is inaccessible.
        /// </summary>
        Fail
    }
}
