using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     An abstract class to override to implement bound user interfaces.
    /// </summary>
    public abstract class BoundUserInterface : IDisposable
    {
        [Dependency] protected internal readonly IEntityManager EntMan = default!;
        [Dependency] protected readonly ISharedPlayerManager PlayerManager = default!;
        protected readonly SharedUserInterfaceSystem UiSystem;

        public bool IsOpened { get; protected set; }

        public readonly Enum UiKey;
        public EntityUid Owner { get; }

        /// <summary>
        /// Additional controls to be disposed when this BUI is disposed.
        /// </summary>
        internal List<IDisposable>? Disposals;

        /// <summary>
        ///     The last received state object sent from the server.
        /// </summary>
        protected internal BoundUserInterfaceState? State { get; internal set; }

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
        [MustCallBase]
        protected internal virtual void Open()
        {
            if (IsOpened)
                return;

            IsOpened = true;
        }

        /// <summary>
        ///     Invoked when the server uses <c>SetState</c>.
        /// </summary>
        protected internal virtual void UpdateState(BoundUserInterfaceState state)
        {
        }

        /// <summary>
        /// Calls <see cref="UpdateState"/> if the supplied state exists and calls <see cref="Update"/>
        /// </summary>
        public void Update<T>() where T : BoundUserInterfaceState
        {
            if (UiSystem.TryGetUiState<T>(Owner, UiKey, out var state))
            {
                UpdateState(state);
            }

            Update();
        }

        /// <summary>
        /// Generic update method called whenever the BUI should update.
        /// </summary>
        public virtual void Update()
        {

        }

        /// <summary>
        /// Helper method that gets called upon prototype reload.
        /// </summary>
        public virtual void OnProtoReload(PrototypesReloadedEventArgs args)
        {

        }

        /// <summary>
        ///     Invoked when the server sends an arbitrary message.
        /// </summary>
        protected internal virtual void ReceiveMessage(BoundUserInterfaceMessage message)
        {
        }

        /// <summary>
        ///     Invoked to close the UI.
        /// </summary>
        public void Close()
        {
            if (!IsOpened)
                return;

            IsOpened = false;
            UiSystem.CloseUi(Owner, UiKey, PlayerManager.LocalEntity, predicted: true);
        }

        /// <summary>
        ///     Sends a message to the server-side UI.
        /// </summary>
        public void SendMessage(BoundUserInterfaceMessage message)
        {
            UiSystem.ClientSendUiMessage(Owner, UiKey, message);
        }

        public void SendPredictedMessage(BoundUserInterfaceMessage message)
        {
            UiSystem.SendPredictedUiMessage(this, message);
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
            if (disposing)
            {
                if (Disposals != null)
                {
                    foreach (var control in Disposals)
                    {
                        control.Dispose();
                    }

                    Disposals = null;
                }
            }
        }
    }
}
