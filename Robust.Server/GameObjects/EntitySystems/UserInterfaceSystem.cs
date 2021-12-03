using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
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

            var transform = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ui.Owner.Owner.Uid);

            var uiPos = transform.WorldPosition;
            var uiMap = transform.MapID;

            foreach (var session in _sessionCache)
            {
                var attachedEntity = session.AttachedEntity;

                // The component manages the set of sessions, so this invalid session should be removed soon.
                if (attachedEntity == null || !IoCManager.Resolve<IEntityManager>().EntityExists(attachedEntity.Uid))
                {
                    continue;
                }

                if (uiMap != IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(attachedEntity.Uid).MapID)
                {
                    ui.Close(session);
                    continue;
                }

                var distanceSquared = (uiPos - IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(attachedEntity.Uid).WorldPosition).LengthSquared;
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
