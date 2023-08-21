using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    [RegisterComponent, ComponentReference(typeof(SharedUserInterfaceComponent))]
    public sealed class ClientUserInterfaceComponent : SharedUserInterfaceComponent
    {
        [ViewVariables]
        internal readonly Dictionary<Enum, PrototypeData> _interfaces = new();

        [ViewVariables]
        public readonly Dictionary<Enum, BoundUserInterface> OpenInterfaces = new();
    }

    /// <summary>
    ///     An abstract class to override to implement bound user interfaces.
    /// </summary>
    public abstract class BoundUserInterface : IDisposable
    {
        [Dependency] protected readonly IEntityManager EntMan = default!;
        protected readonly UserInterfaceSystem UiSystem = default!;

        public readonly Enum UiKey;
        public EntityUid Owner { get; }

        /// <summary>
        ///     The last received state object sent from the server.
        /// </summary>
        protected BoundUserInterfaceState? State { get; private set; }

        protected BoundUserInterface(EntityUid owner, Enum uiKey)
        {
            IoCManager.InjectDependencies(this);
            UiSystem = EntMan.System<UserInterfaceSystem>();

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
            UiSystem.TryCloseUi(Owner, UiKey);
        }

        /// <summary>
        ///     Sends a message to the server-side UI.
        /// </summary>
        public void SendMessage(BoundUserInterfaceMessage message)
        {
            UiSystem.SendUiMessage(this, message);
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
