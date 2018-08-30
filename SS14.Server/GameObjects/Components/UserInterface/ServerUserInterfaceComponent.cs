using System;
using System.Collections.Generic;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.UserInterface;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Serialization;

namespace SS14.Server.GameObjects.Components.UserInterface
{
    /// <summary>
    ///     Contains a collection of entity-bound user interfaces that can be opened per client.
    ///     Bound user interfaces are indexed with an enum or string key identifier.
    /// </summary>
    /// <seealso cref="BoundUserInterface"/>
    public sealed class ServerUserInterfaceComponent : SharedUserInterfaceComponent
    {
        private readonly Dictionary<object, BoundUserInterface> _interfaces =
            new Dictionary<object, BoundUserInterface>();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            if (!serializer.Reading)
            {
                return;
            }

            var data = serializer.ReadDataFieldCached("interfaces", new List<PrototypeData>());
            foreach (var prototypeData in data)
            {
                _interfaces[prototypeData.UiKey] = new BoundUserInterface(prototypeData.UiKey, this);
            }
        }

        public BoundUserInterface GetBoundUserInterface(object uiKey)
        {
            return _interfaces[uiKey];
        }

        public bool TryGetBoundUserInterface(object uiKey, out BoundUserInterface boundUserInterface)
        {
            return _interfaces.TryGetValue(uiKey, out boundUserInterface);
        }

        public bool HasBoundUserInterface(object uiKey)
        {
            return _interfaces.ContainsKey(uiKey);
        }

        internal void SendToSession(IPlayerSession session, BoundUserInterfaceMessage message, object uiKey)
        {
            SendNetworkMessage(new BoundInterfaceMessageWrapMessage(message, uiKey), session.ConnectedClient);
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null,
            IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case BoundInterfaceMessageWrapMessage wrapped:
                    if (netChannel == null)
                    {
                        throw new ArgumentNullException(nameof(netChannel));
                    }

                    var session = IoCManager.Resolve<IPlayerManager>().GetSessionById(netChannel.SessionId);
                    if (!_interfaces.TryGetValue(wrapped.UiKey, out var @interface))
                    {
                        Logger.DebugS("go.comp.ui", "Got BoundInterfaceMessageWrapMessage for unknown UI key: {0}",
                            wrapped.UiKey);
                        return;
                    }

                    @interface.ReceiveMessage(wrapped.Message, session);
                    break;
            }
        }
    }

    /// <summary>
    ///     Represents an entity-bound interface that can be opened by multiple players at once.
    /// </summary>
    public sealed class BoundUserInterface
    {
        public object UiKey { get; }
        public ServerUserInterfaceComponent Owner { get; }
        private readonly HashSet<IPlayerSession> _subscribedSessions = new HashSet<IPlayerSession>();
        private BoundUserInterfaceState _lastState;

        public event Action<BoundUserInterfaceMessage> OnReceiveMessage;

        public BoundUserInterface(object uiKey, ServerUserInterfaceComponent owner)
        {
            UiKey = uiKey;
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
        public void SetState(BoundUserInterfaceState state)
        {
            SendMessage(new UpdateBoundStateMessage(state));
            _lastState = state;
        }

        /// <summary>
        ///     Opens this interface for a specific client.
        /// </summary>
        /// <param name="session">The player session to open the UI on.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown if the session's status is <c>Connecting</c> or <c>Disconnected</c>
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if <see cref="session"/> is null.</exception>
        public void Open(IPlayerSession session)
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
                return;
            }

            _subscribedSessions.Add(session);
            SendMessage(new OpenBoundInterfaceMessage(), session);
            if (_lastState != null)
            {
                SendMessage(new UpdateBoundStateMessage(_lastState));
            }

            session.PlayerStatusChanged += (sender, args) =>
            {
                if (args.NewStatus == SessionStatus.Disconnected)
                {
                    _subscribedSessions.Remove(session);
                }
            };
        }

        /// <summary>
        ///     Close this interface for a specific client.
        /// </summary>
        /// <param name="session">The session to close the UI on.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="session"/> is null.</exception>
        public void Close(IPlayerSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (_subscribedSessions.Contains(session))
            {
                return;
            }

            SendMessage(new CloseBoundInterfaceMessage(), session);
            _subscribedSessions.Remove(session);
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

        internal void ReceiveMessage(BoundUserInterfaceMessage wrappedMessage, IPlayerSession session)
        {
            if (!_subscribedSessions.Contains(session))
            {
                Logger.DebugS("go.comp.ui", "Got message from session not subscribed to us.");
                return;
            }

            switch (wrappedMessage)
            {
                case CloseBoundInterfaceMessage _:
                    _subscribedSessions.Remove(session);
                    break;

                default:
                    OnReceiveMessage?.Invoke(wrappedMessage);
                    break;
            }
        }

        private void AssertContains(IPlayerSession session)
        {
            if (!SessionHasOpen(session))
            {
                throw new ArgumentException("Player session does not have this UI open.");
            }
        }
    }
}
