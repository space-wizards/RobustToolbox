using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    [UsedImplicitly]
    public class UserInterfaceSystem : SharedUserInterfaceSystem
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

        private void OnMessageReceived(BoundUIWrapMessage msg, EntitySessionEventArgs args)
        {
            var uid = msg.Entity;
            if (!EntityManager.TryGetComponent<ServerUserInterfaceComponent>(uid, out var uiComp))
                return;

            var message = msg.Message;
            message.Session = args.SenderSession;
            message.Entity = uid;
            message.UiKey = msg.UiKey;

            // Raise as object so the correct type is used.
            RaiseLocalEvent(uid, (object)message);

            uiComp.ReceiveMessage((IPlayerSession) args.SenderSession, msg);
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

            var transform = ui.Owner.Owner.Transform;

            var uiPos = transform.WorldPosition;
            var uiMap = transform.MapID;

            foreach (var session in _sessionCache)
            {
                var attachedEntity = session.AttachedEntity;

                // The component manages the set of sessions, so this invalid session should be removed soon.
                if (attachedEntity == null || !attachedEntity.IsValid())
                {
                    continue;
                }

                if (uiMap != attachedEntity.Transform.MapID)
                {
                    ui.Close(session);
                    continue;
                }

                var distanceSquared = (uiPos - attachedEntity.Transform.WorldPosition).LengthSquared;
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

        public bool HasUi(EntityUid uid, object uiKey)
        {
            if (!EntityManager.TryGetComponent<ServerUserInterfaceComponent>(uid, out var ui))
                return false;

            return ui.HasBoundUserInterface(uiKey);
        }

        public BoundUserInterface GetUi(EntityUid uid, object uiKey)
        {
            return EntityManager.GetComponent<ServerUserInterfaceComponent>(uid).GetBoundUserInterface(uiKey);
        }

        public BoundUserInterface? GetUiOrNull(EntityUid uid, object uiKey)
        {
            return TryGetUi(uid, uiKey, out var ui)
                ? ui
                : null;
        }

        public bool TryGetUi(EntityUid uid, object uiKey, [NotNullWhen(true)] out BoundUserInterface? ui)
        {
            ui = null;

            return EntityManager.TryGetComponent(uid, out ServerUserInterfaceComponent uiComp)
                   && uiComp.TryGetBoundUserInterface(uiKey, out ui);
        }

        public bool TrySetUiState(EntityUid uid, object uiKey, BoundUserInterfaceState state, IPlayerSession? session = null)
        {
            if (!TryGetUi(uid, uiKey, out var ui))
                return false;

            ui.SetState(state, session);
            return true;
        }

        public bool TryToggleUi(EntityUid uid, object uiKey, IPlayerSession session)
        {
            if (!TryGetUi(uid, uiKey, out var ui))
                return false;

            ui.Toggle(session);
            return true;
        }

        public bool TryOpen(EntityUid uid, object uiKey, IPlayerSession session)
        {
            if (!TryGetUi(uid, uiKey, out var ui))
                return false;

            return ui.Open(session);
        }

        public bool TryClose(EntityUid uid, object uiKey, IPlayerSession session)
        {
            if (!TryGetUi(uid, uiKey, out var ui))
                return false;

            return ui.Close(session);
        }

        public bool TryCloseAll(EntityUid uid, object uiKey)
        {
            if (!TryGetUi(uid, uiKey, out var ui))
                return false;

            ui.CloseAll();
            return true;
        }

        public bool SessionHasOpenUi(EntityUid uid, object uiKey, IPlayerSession session)
        {
            if (!TryGetUi(uid, uiKey, out var ui))
                return false;

            return ui.SessionHasOpen(session);
        }

        public bool TrySendUiMessage(EntityUid uid, object uiKey, BoundUserInterfaceMessage message)
        {
            if (!TryGetUi(uid, uiKey, out var ui))
                return false;

            ui.SendMessage(message);
            return true;
        }

        public bool TrySendUiMessage(EntityUid uid, object uiKey, BoundUserInterfaceMessage message, IPlayerSession session)
        {
            if (!TryGetUi(uid, uiKey, out var ui))
                return false;

            try
            {
                ui.SendMessage(message, session);
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
