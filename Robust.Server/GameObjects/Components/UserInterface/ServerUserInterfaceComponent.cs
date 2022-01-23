using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using static Robust.Shared.GameObjects.SharedUserInterfaceComponent;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     Contains a collection of entity-bound user interfaces that can be opened per client.
    ///     Bound user interfaces are indexed with an enum or string key identifier.
    /// </summary>
    /// <seealso cref="BoundUserInterface"/>
    [PublicAPI]
    [ComponentReference(typeof(SharedUserInterfaceComponent))]
    public sealed class ServerUserInterfaceComponent : SharedUserInterfaceComponent, ISerializationHooks
    {
        private readonly Dictionary<object, BoundUserInterface> _interfaces =
            new();

        [DataField("interfaces", readOnly: true)]
        private List<PrototypeData> _interfaceData = new();

        /// <summary>
        ///     Enumeration of all the interfaces this component provides.
        /// </summary>
        public IEnumerable<BoundUserInterface> Interfaces => _interfaces.Values;

        void ISerializationHooks.AfterDeserialization()
        {
            _interfaces.Clear();

            foreach (var prototypeData in _interfaceData)
            {
                _interfaces[prototypeData.UiKey] = new BoundUserInterface(prototypeData, this);
            }
        }

        public BoundUserInterface GetBoundUserInterface(object uiKey)
        {
            return _interfaces[uiKey];
        }

        public bool TryGetBoundUserInterface(object uiKey,
            [NotNullWhen(true)] out BoundUserInterface? boundUserInterface)
        {
            return _interfaces.TryGetValue(uiKey, out boundUserInterface);
        }

        public BoundUserInterface? GetBoundUserInterfaceOrNull(object uiKey)
        {
            return TryGetBoundUserInterface(uiKey, out var boundUserInterface)
                ? boundUserInterface
                : null;
        }

        public bool HasBoundUserInterface(object uiKey)
        {
            return _interfaces.ContainsKey(uiKey);
        }

        internal void SendToSession(IPlayerSession session, BoundUserInterfaceMessage message, object uiKey)
        {
            EntitySystem.Get<UserInterfaceSystem>()
                .SendTo(session, new BoundUIWrapMessage(Owner, message, uiKey));
        }
    }

    /// <summary>
    ///     Represents an entity-bound interface that can be opened by multiple players at once.
    /// </summary>
    [PublicAPI]
    public sealed class BoundUserInterface
    {
        private bool _isActive;

        public object UiKey { get; }
        public ServerUserInterfaceComponent Owner { get; }
        private readonly HashSet<IPlayerSession> _subscribedSessions = new();
        private BoundUserInterfaceState? _lastState;
        public bool RequireInputValidation;

        private bool _stateDirty;

        private readonly Dictionary<IPlayerSession, BoundUserInterfaceState> _playerStateOverrides =
            new();

        /// <summary>
        ///     All of the sessions currently subscribed to this UserInterface.
        /// </summary>
        public IReadOnlyCollection<IPlayerSession> SubscribedSessions => _subscribedSessions;

        public event Action<ServerBoundUserInterfaceMessage>? OnReceiveMessage;
        public event Action<IPlayerSession>? OnClosed;

        public BoundUserInterface(PrototypeData data, ServerUserInterfaceComponent owner)
        {
            RequireInputValidation = data.RequireInputValidation;
            UiKey = data.UiKey;
            Owner = owner;
        }

        /// <summary>
        ///     Sets a state. This can be used for stateful UI updating, which can be easier to implement,
        ///     but is more costly on bandwidth.
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
        public void SetState(BoundUserInterfaceState state, IPlayerSession? session = null)
        {
            if (session == null)
            {
                _lastState = state;
                _playerStateOverrides.Clear();
            }
            else
            {
                _playerStateOverrides[session] = state;
            }

            _stateDirty = true;
        }


        /// <summary>
        ///     Switches between closed and open for a specific client.
        /// </summary>
        /// <param name="session">The player session to toggle the UI on.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown if the session's status is <c>Connecting</c> or <c>Disconnected</c>
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if <see cref="session"/> is null.</exception>
        public void Toggle(IPlayerSession session)
        {
            if (_subscribedSessions.Contains(session))
            {
                Close(session);
            }
            else
            {
                Open(session);
            }
        }


        /// <summary>
        ///     Opens this interface for a specific client.
        /// </summary>
        /// <param name="session">The player session to open the UI on.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown if the session's status is <c>Connecting</c> or <c>Disconnected</c>
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if <see cref="session"/> is null.</exception>
        public bool Open(IPlayerSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.Status == SessionStatus.Connecting || session.Status == SessionStatus.Disconnected)
            {
                throw new ArgumentException("Invalid session status.", nameof(session));
            }

            if (_subscribedSessions.Contains(session))
            {
                return false;
            }

            _subscribedSessions.Add(session);
            SendMessage(new OpenBoundInterfaceMessage(), session);
            if (_lastState != null)
            {
                SendMessage(new UpdateBoundStateMessage(_lastState));
            }

            if (!_isActive)
            {
                _isActive = true;

                EntitySystem.Get<UserInterfaceSystem>()
                    .ActivateInterface(this);
            }

            session.PlayerStatusChanged += OnSessionOnPlayerStatusChanged;
            return true;
        }

        private void OnSessionOnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            if (args.NewStatus == SessionStatus.Disconnected)
            {
                CloseShared(args.Session);
            }
        }

        /// <summary>
        ///     Close this interface for a specific client.
        /// </summary>
        /// <param name="session">The session to close the UI on.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="session"/> is null.</exception>
        public bool Close(IPlayerSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (!_subscribedSessions.Contains(session))
            {
                return false;
            }

            var msg = new CloseBoundInterfaceMessage();
            SendMessage(msg, session);
            CloseShared(session);
            return true;
        }

        public void CloseShared(IPlayerSession session)
        {
            var owner = Owner.Owner;
            IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(owner, new BoundUIClosedEvent(UiKey, owner, session));
            OnClosed?.Invoke(session);
            _subscribedSessions.Remove(session);
            _playerStateOverrides.Remove(session);
            session.PlayerStatusChanged -= OnSessionOnPlayerStatusChanged;

            if (_subscribedSessions.Count == 0)
            {
                EntitySystem.Get<UserInterfaceSystem>()
                    .DeactivateInterface(this);

                _isActive = false;
            }
        }

        /// <summary>
        ///     Closes this interface for any clients that have it open.
        /// </summary>
        public void CloseAll()
        {
            foreach (var session in _subscribedSessions.ToArray())
                Close(session);
        }

        /// <summary>
        ///     Returns whether or not a session has this UI open.
        /// </summary>
        /// <param name="session">The session to check.</param>
        /// <returns>True if the player has this UI open, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="session"/> is null.</exception>
        public bool SessionHasOpen(IPlayerSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            return _subscribedSessions.Contains(session);
        }

        /// <summary>
        ///     Sends a message to ALL sessions that currently have the UI open.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
        public void SendMessage(BoundUserInterfaceMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            foreach (var session in _subscribedSessions)
            {
                Owner.SendToSession(session, message, UiKey);
            }
        }

        /// <summary>
        ///     Sends a message to a specific session.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="session">The session to send the message to.</param>
        /// <exception cref="ArgumentNullException">Thrown if either argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the session does not have this UI open.</exception>
        public void SendMessage(BoundUserInterfaceMessage message, IPlayerSession session)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            AssertContains(session);

            Owner.SendToSession(session, message, UiKey);
        }

        internal void ReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            OnReceiveMessage?.Invoke(message);
        }

        private void AssertContains(IPlayerSession session)
        {
            if (!SessionHasOpen(session))
            {
                throw new ArgumentException("Player session does not have this UI open.");
            }
        }

        public void DispatchPendingState()
        {
            if (!_stateDirty)
            {
                return;
            }

            foreach (var playerSession in _subscribedSessions)
            {
                if (!_playerStateOverrides.ContainsKey(playerSession) && _lastState != null)
                {
                    SendMessage(new UpdateBoundStateMessage(_lastState), playerSession);
                }
            }

            foreach (var (player, state) in _playerStateOverrides)
            {
                SendMessage(new UpdateBoundStateMessage(state), player);
            }

            _stateDirty = false;
        }
    }

    [PublicAPI]
    public class ServerBoundUserInterfaceMessage
    {
        public BoundUserInterfaceMessage Message { get; }
        public IPlayerSession Session { get; }

        public ServerBoundUserInterfaceMessage(BoundUserInterfaceMessage message, IPlayerSession session)
        {
            Message = message;
            Session = session;
        }
    }
}
