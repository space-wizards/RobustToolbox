using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using static Robust.Shared.GameObjects.SharedUserInterfaceComponent;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     Contains a collection of entity-bound user interfaces that can be opened per client.
    ///     Bound user interfaces are indexed with an enum or string key identifier.
    /// </summary>
    /// <seealso cref="BoundUserInterface"/>
    [PublicAPI]
    [ComponentReference(typeof(SharedUserInterfaceComponent))]
    public sealed class ServerUserInterfaceComponent : SharedUserInterfaceComponent, ISerializationHooks
    {
        internal readonly Dictionary<Enum, BoundUserInterface> _interfaces =
            new();

        public IReadOnlyDictionary<Enum, BoundUserInterface> Interfaces => _interfaces;

        void ISerializationHooks.AfterDeserialization()
        {
            _interfaces.Clear();

            foreach (var prototypeData in _interfaceData)
            {
                _interfaces[prototypeData.UiKey] = new BoundUserInterface(prototypeData, this);
            }
        }
    }

    [RegisterComponent]
    public sealed class ActiveUserInterfaceComponent : Component
    {
        public HashSet<BoundUserInterface> Interfaces = new();
    }

    /// <summary>
    ///     Represents an entity-bound interface that can be opened by multiple players at once.
    /// </summary>
    [PublicAPI]
    public sealed class BoundUserInterface
    {
        public float InteractionRangeSqrd;

        public Enum UiKey { get; }
        public ServerUserInterfaceComponent Component { get; }
        public EntityUid Owner => Component.Owner;

        internal readonly HashSet<IPlayerSession> _subscribedSessions = new();
        internal BoundUIWrapMessage? LastStateMsg;
        public bool RequireInputValidation;

        internal bool StateDirty;

        internal readonly Dictionary<IPlayerSession, BoundUIWrapMessage> PlayerStateOverrides =
            new();

        /// <summary>
        ///     All of the sessions currently subscribed to this UserInterface.
        /// </summary>
        public IReadOnlySet<IPlayerSession> SubscribedSessions => _subscribedSessions;

        [Obsolete("Use system events")]
        public event Action<ServerBoundUserInterfaceMessage>? OnReceiveMessage;

        public BoundUserInterface(PrototypeData data, ServerUserInterfaceComponent owner)
        {
            RequireInputValidation = data.RequireInputValidation;
            UiKey = data.UiKey;
            Component = owner;

            // One Abs(), because negative values imply no limit
            InteractionRangeSqrd = data.InteractionRange * MathF.Abs(data.InteractionRange);
        }

        [Obsolete("Use UserInterfaceSystem")]
        public void SetState(BoundUserInterfaceState state, IPlayerSession? session = null, bool clearOverrides = true)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<UserInterfaceSystem>().SetUiState(this, state, session, clearOverrides);
        }

        [Obsolete("Use UserInterfaceSystem")]
        public void Toggle(IPlayerSession session)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<UserInterfaceSystem>().ToggleUi(this, session);
        }

        [Obsolete("Use UserInterfaceSystem")]
        public bool Open(IPlayerSession session)
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<UserInterfaceSystem>().OpenUi(this, session);
        }

        [Obsolete("Use UserInterfaceSystem")]
        public bool Close(IPlayerSession session)
        {
            return IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<UserInterfaceSystem>().CloseUi(this, session);
        }

        [Obsolete("Use UserInterfaceSystem")]
        public void CloseAll()
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<UserInterfaceSystem>().CloseAll(this);
        }

        [Obsolete("Just check SubscribedSessions.Contains")]
        public bool SessionHasOpen(IPlayerSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            return _subscribedSessions.Contains(session);
        }

        [Obsolete("Use UserInterfaceSystem")]
        public void SendMessage(BoundUserInterfaceMessage message)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<UserInterfaceSystem>().SendUiMessage(this, message);
        }

        [Obsolete("Use UserInterfaceSystem")]
        public void SendMessage(BoundUserInterfaceMessage message, IPlayerSession session)
        {
            IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<UserInterfaceSystem>().TrySendUiMessage(this, message, session);
        }

        internal void InvokeOnReceiveMessage(ServerBoundUserInterfaceMessage message)
        {
            OnReceiveMessage?.Invoke(message);
        }
    }

    [PublicAPI]
    public sealed class ServerBoundUserInterfaceMessage
    {
        public BoundUserInterfaceMessage Message { get; }
        public IPlayerSession Session { get; }

        public ServerBoundUserInterfaceMessage(BoundUserInterfaceMessage message, IPlayerSession session)
        {
            Message = message;
            Session = session;
        }
    }
}
