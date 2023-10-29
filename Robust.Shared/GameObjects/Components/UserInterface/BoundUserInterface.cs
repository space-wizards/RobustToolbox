using System;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     An abstract class to override to implement bound user interfaces.
    /// </summary>
    public abstract class BoundUserInterface : IDisposable
    {
        [Dependency] protected readonly IEntityManager EntMan = default!;
        [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
        protected readonly SharedUserInterfaceSystem UiSystem;

        public readonly Enum UiKey;
        public EntityUid Owner { get; }

        /// <summary>
        ///     The last received state object sent from the server.
        /// </summary>
        protected BoundUserInterfaceState? State { get; private set; }

        protected BoundUserInterface(EntityUid owner, Enum uiKey)
        {
            IoCManager.InjectDependencies(this);
            UiSystem = EntMan.System<SharedUserInterfaceSystem>();

            Owner = owner;
            UiKey = uiKey;
        }

        /// <summary>
        ///     Invoked when the UI is opened.
        ///     Do all creation and opening of things like windows in here.
        /// </summary>
        protected internal virtual void Open()
        {
        }

        /// <summary>
        ///     Invoked when the server uses <c>SetState</c>.
        /// </summary>
        protected virtual void UpdateState(BoundUserInterfaceState state)
        {
        }

        /// <summary>
        ///     Invoked when the server sends an arbitrary message.
        /// </summary>
        protected virtual void ReceiveMessage(BoundUserInterfaceMessage message)
        {
        }

        /// <summary>
        ///     Invoked to close the UI.
        /// </summary>
        public void Close()
        {
            UiSystem.TryCloseUi(_playerManager.LocalSession, Owner, UiKey);
        }

        /// <summary>
        ///     Sends a message to the server-side UI.
        /// </summary>
        public void SendMessage(BoundUserInterfaceMessage message)
        {
            UiSystem.SendUiMessage(this, message);
        }

        public void SendPredictedMessage(BoundUserInterfaceMessage message)
        {
            UiSystem.SendPredictedUiMessage(this, message);
        }

        internal void InternalReceiveMessage(BoundUserInterfaceMessage message)
        {
            switch (message)
            {
                case UpdateBoundStateMessage updateBoundStateMessage:
                    State = updateBoundStateMessage.State;
                    UpdateState(State);
                    break;
                default:
                    ReceiveMessage(message);
                    break;
            }
        }

        ~BoundUserInterface()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
