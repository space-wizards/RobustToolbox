using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    [UsedImplicitly]
    public sealed class UserInterfaceSystem : SharedUserInterfaceSystem
    {
        private const float MaxWindowRange = 2;
        private const float MaxWindowRangeSquared = MaxWindowRange * MaxWindowRange;

        private readonly List<IPlayerSession> _sessionCache = new();

        // List of all bound user interfaces that have at least one player looking at them.
        [ViewVariables]
        private readonly List<BoundUserInterface> _activeInterfaces = new();

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<BoundUIWrapMessage>(OnMessageReceived);
            SubscribeLocalEvent<ServerUserInterfaceComponent, ComponentShutdown>(OnUserInterfaceShutdown);
        }

        private void OnUserInterfaceShutdown(EntityUid uid, ServerUserInterfaceComponent component, ComponentShutdown args)
        {
            foreach (var bui in component.Interfaces)
            {
                DeactivateInterface(bui);
            }
        }

        internal void SendTo(IPlayerSession session, BoundUIWrapMessage msg)
        {
            RaiseNetworkEvent(msg, session.ConnectedClient);
        }

        /// <summary>
        ///     Validates the received message, and then pass it onto systems/components
        /// </summary>
        private void OnMessageReceived(BoundUIWrapMessage msg, EntitySessionEventArgs args)
        {
            var uid = msg.Entity;
            if (!TryComp(uid, out ServerUserInterfaceComponent? uiComp) || args.SenderSession is not IPlayerSession session)
                return;

            if (!uiComp.TryGetBoundUserInterface(msg.UiKey, out var ui))
            {
                Logger.DebugS("go.comp.ui", "Got BoundInterfaceMessageWrapMessage for unknown UI key: {0}", msg.UiKey);
                return;
            }

            if (!ui.SessionHasOpen(session))
            {
                Logger.DebugS("go.comp.ui", $"UI {msg.UiKey} got BoundInterfaceMessageWrapMessage from a client who was not subscribed: {session}", msg.UiKey);
                return;
            }

            // if they want to close the UI, we can go home early.
            if (msg.Message is CloseBoundInterfaceMessage)
            {
                ui.CloseShared(session);
                return;
            }

            // verify that the user is allowed to press buttons on this UI:
            if (ui.RequireInputValidation)
            {
                var attempt = new BoundUserInterfaceMessageAttempt(args.SenderSession, uid, msg.UiKey);
                RaiseLocalEvent(attempt);
                if (attempt.Cancelled)
                    return;
            }

            // get the wrapped message and populate it with the sender & UI key information.
            var message = msg.Message;
            message.Session = args.SenderSession;
            message.Entity = uid;
            message.UiKey = msg.UiKey;

            // Raise as object so the correct type is used.
            RaiseLocalEvent(uid, (object)message);

            // Once we have populated our message's wrapped message, we will wrap it up into a message that can be sent
            // to old component-code.
            var WrappedUnwrappedMessageMessageMessage = new ServerBoundUserInterfaceMessage(message, session);
            ui.ReceiveMessage(WrappedUnwrappedMessageMessageMessage);
        }

        /// <inheritdoc />
        public override void Update(float frameTime)
        {
            foreach (var userInterface in _activeInterfaces.ToList())
            {
                CheckRange(userInterface);
                userInterface.DispatchPendingState();
            }
        }

        /// <summary>
        ///     Verify that the subscribed clients are still in range of the interface.
        /// </summary>
        private void CheckRange(BoundUserInterface ui)
        {
            // We have to cache the set of sessions because Unsubscribe modifies the original.
            _sessionCache.Clear();
            _sessionCache.AddRange(ui.SubscribedSessions);

            var transform = EntityManager.GetComponent<TransformComponent>(ui.Owner.Owner);

            var uiPos = transform.WorldPosition;
            var uiMap = transform.MapID;

            foreach (var session in _sessionCache)
            {
                var attachedEntityTransform = session.AttachedEntityTransform;

                // The component manages the set of sessions, so this invalid session should be removed soon.
                if (attachedEntityTransform == null)
                {
                    continue;
                }

                if (uiMap != attachedEntityTransform.MapID)
                {
                    ui.Close(session);
                    continue;
                }

                var distanceSquared = (uiPos - attachedEntityTransform.WorldPosition).LengthSquared;
                if (distanceSquared > MaxWindowRangeSquared)
                {
                    ui.Close(session);
                }
            }
        }

        internal void DeactivateInterface(BoundUserInterface userInterface)
        {
            _activeInterfaces.Remove(userInterface);
        }

        internal void ActivateInterface(BoundUserInterface userInterface)
        {
            _activeInterfaces.Add(userInterface);
        }

        #region Proxy Methods

        public bool HasUi(EntityUid uid, object uiKey, ServerUserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                return false;

            return ui.HasBoundUserInterface(uiKey);
        }

        public BoundUserInterface GetUi(EntityUid uid, object uiKey, ServerUserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                throw new InvalidOperationException($"Cannot get {typeof(BoundUserInterface)} from an entity without {typeof(ServerUserInterfaceComponent)}!");

            return ui.GetBoundUserInterface(uiKey);
        }

        public BoundUserInterface? GetUiOrNull(EntityUid uid, object uiKey, ServerUserInterfaceComponent? ui = null)
        {
            return TryGetUi(uid, uiKey, out var bui)
                ? bui
                : null;
        }

        public bool TryGetUi(EntityUid uid, object uiKey, [NotNullWhen(true)] out BoundUserInterface? bui, ServerUserInterfaceComponent? ui = null)
        {
            bui = null;

            return Resolve(uid, ref ui) && ui.TryGetBoundUserInterface(uiKey, out bui);
        }

        public bool IsUiOpen(EntityUid uid, object uiKey, ServerUserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                return false;

            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            return bui.SubscribedSessions.Count > 0;
        }

        public bool TrySetUiState(EntityUid uid, object uiKey, BoundUserInterfaceState state, IPlayerSession? session = null, ServerUserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                return false;

            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            bui.SetState(state, session);
            return true;
        }

        public bool TryToggleUi(EntityUid uid, object uiKey, IPlayerSession session, ServerUserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                return false;

            if (!TryGetUi(uid, uiKey, out var bui))
                return false;

            bui.Toggle(session);
            return true;
        }

        public bool TryOpen(EntityUid uid, object uiKey, IPlayerSession session, ServerUserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                return false;

            if (!TryGetUi(uid, uiKey, out var bui))
                return false;

            return bui.Open(session);
        }

        public bool TryClose(EntityUid uid, object uiKey, IPlayerSession session, ServerUserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                return false;

            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            return bui.Close(session);
        }

        public bool TryCloseAll(EntityUid uid, object uiKey, ServerUserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                return false;

            if (!TryGetUi(uid, uiKey, out var bui, ui))
                return false;

            bui.CloseAll();
            return true;
        }

        public bool SessionHasOpenUi(EntityUid uid, object uiKey, IPlayerSession session, ServerUserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                return false;

            if (!TryGetUi(uid, uiKey, out var bui))
                return false;

            return bui.SessionHasOpen(session);
        }

        public bool TrySendUiMessage(EntityUid uid, object uiKey, BoundUserInterfaceMessage message, ServerUserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                return false;

            if (!TryGetUi(uid, uiKey, out var bui))
                return false;

            bui.SendMessage(message);
            return true;
        }

        public bool TrySendUiMessage(EntityUid uid, object uiKey, BoundUserInterfaceMessage message, IPlayerSession session, ServerUserInterfaceComponent? ui = null)
        {
            if (!Resolve(uid, ref ui))
                return false;

            if (!TryGetUi(uid, uiKey, out var bui))
                return false;

            try
            {
                bui.SendMessage(message, session);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
