using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.UserInterface;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using IComponent = Robust.Shared.Interfaces.GameObjects.IComponent;

namespace Robust.Client.GameObjects.Components.UserInterface
{
    public class ClientUserInterfaceComponent : SharedUserInterfaceComponent
    {
        private readonly Dictionary<object, BoundUserInterface> _openInterfaces =
            new();

        private Dictionary<object, PrototypeData> _interfaceData = new();

        [DataField("interfaces")]
        private List<PrototypeData> _dataReceiver
        {
            set
            {
                _interfaceData.Clear();
                foreach (var data in value)
                {
                    _interfaceData[data.UiKey] = data;
                }
            }
            get
            {
                return _interfaceData.Values.ToList();
            }
        }

        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IDynamicTypeFactory _dynamicTypeFactory = default!;

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel netChannel,
            ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, netChannel, session);

            switch (message)
            {
                case BoundInterfaceMessageWrapMessage wrapped:
                    // Double nested switches who needs readability anyways.
                    switch (wrapped.Message)
                    {
                        case OpenBoundInterfaceMessage _:
                            if (_openInterfaces.ContainsKey(wrapped.UiKey))
                            {
                                return;
                            }

                            OpenInterface(wrapped);
                            break;

                        case CloseBoundInterfaceMessage _:
                            Close(wrapped.UiKey, true);
                            break;

                        default:
                            if (_openInterfaces.TryGetValue(wrapped.UiKey, out var bi))
                            {
                                bi.InternalReceiveMessage(wrapped.Message);
                            }
                            break;
                    }

                    break;
            }
        }

        private void OpenInterface(BoundInterfaceMessageWrapMessage wrapped)
        {
            var data = _interfaceData[wrapped.UiKey];
            // TODO: This type should be cached, but I'm too lazy.
            var type = _reflectionManager.LooseGetType(data.ClientType);
            var boundInterface = (BoundUserInterface) _dynamicTypeFactory.CreateInstance(type, new[]{this, wrapped.UiKey});
            boundInterface.Open();
            _openInterfaces[wrapped.UiKey] = boundInterface;
        }

        internal void Close(object uiKey, bool remoteCall)
        {
            if (!_openInterfaces.TryGetValue(uiKey, out var boundUserInterface))
            {
                return;
            }

            if(!remoteCall)
                SendMessage(new CloseBoundInterfaceMessage(), uiKey);
            _openInterfaces.Remove(uiKey);
            boundUserInterface.Dispose();
        }

        internal void SendMessage(BoundUserInterfaceMessage message, object uiKey)
        {
            SendNetworkMessage(new BoundInterfaceMessageWrapMessage(message, uiKey));
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
