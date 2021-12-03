using System;
using System.Collections.Generic;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    [ComponentReference(typeof(SharedUserInterfaceComponent))]
    public class ClientUserInterfaceComponent : SharedUserInterfaceComponent, ISerializationHooks
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IDynamicTypeFactory _dynamicTypeFactory = default!;

        private readonly Dictionary<object, BoundUserInterface> _openInterfaces =
            new();

        private readonly Dictionary<object, PrototypeData> _interfaces = new();

        [DataField("interfaces", readOnly: true)]
        private List<PrototypeData> _interfaceData = new();

        [ViewVariables]
        public IEnumerable<BoundUserInterface> Interfaces => _openInterfaces.Values;

        void ISerializationHooks.AfterDeserialization()
        {
            _interfaces.Clear();

            foreach (var data in _interfaceData)
            {
                _interfaces[data.UiKey] = data;
            }
        }

        internal void MessageReceived(BoundUIWrapMessage msg)
        {
            switch (msg.Message)
            {
                case OpenBoundInterfaceMessage _:
                    if (_openInterfaces.ContainsKey(msg.UiKey))
                    {
                        return;
                    }

                    OpenInterface(msg);
                    break;

                case CloseBoundInterfaceMessage _:
                    Close(msg.UiKey, true);
                    break;

                default:
                    if (_openInterfaces.TryGetValue(msg.UiKey, out var bi))
                    {
                        bi.InternalReceiveMessage(msg.Message);
                    }

                    break;
            }
        }

        private void OpenInterface(BoundUIWrapMessage wrapped)
        {
            var data = _interfaces[wrapped.UiKey];
            // TODO: This type should be cached, but I'm too lazy.
            var type = _reflectionManager.LooseGetType(data.ClientType);
            var boundInterface =
                (BoundUserInterface) _dynamicTypeFactory.CreateInstance(type, new[] {this, wrapped.UiKey});
            boundInterface.Open();
            _openInterfaces[wrapped.UiKey] = boundInterface;
        }

        internal void Close(object uiKey, bool remoteCall)
        {
            if (!_openInterfaces.TryGetValue(uiKey, out var boundUserInterface))
            {
                return;
            }

            if (!remoteCall)
                SendMessage(new CloseBoundInterfaceMessage(), uiKey);
            _openInterfaces.Remove(uiKey);
            boundUserInterface.Dispose();

            var playerSession = IoCManager.Resolve<IPlayerManager>().LocalPlayer?.Session;
            if(playerSession != null)
                IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(Owner, new BoundUIClosedEvent(uiKey, Owner, playerSession));
        }

        internal void SendMessage(BoundUserInterfaceMessage message, object uiKey)
        {
            EntitySystem.Get<UserInterfaceSystem>()
                .Send(new BoundUIWrapMessage(Owner, message, uiKey));
        }
    }

    /// <summary>
    ///     An abstract class to override to implement bound user interfaces.
    /// </summary>
    public abstract class BoundUserInterface : IDisposable
    {
        protected ClientUserInterfaceComponent Owner { get; }
        protected object UiKey { get; }

        /// <summary>
        ///     The last received state object sent from the server.
        /// </summary>
        protected BoundUserInterfaceState? State { get; private set; }

        protected BoundUserInterface(ClientUserInterfaceComponent owner, object uiKey)
        {
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
        protected void Close()
        {
            Owner.Close(UiKey, false);
        }

        /// <summary>
        ///     Sends a message to the server-side UI.
        /// </summary>
        protected void SendMessage(BoundUserInterfaceMessage message)
        {
            Owner.SendMessage(message, UiKey);
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
