using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    [UsedImplicitly]
    internal class UserInterfaceSystem : EntitySystem
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
            if (!ComponentManager.TryGetComponent<ServerUserInterfaceComponent>(msg.Entity, out var uiComp))
                return;

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
    }
}
