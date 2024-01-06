using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class UserInterfaceComponent : Component
    {
        // TODO: Obviously clean this shit up, I just moved it into shared.

        [ViewVariables] public readonly Dictionary<Enum, BoundUserInterface> OpenInterfaces = new();

        [ViewVariables] public readonly Dictionary<Enum, PlayerBoundUserInterface> Interfaces = new();

        public Dictionary<Enum, PrototypeData> MappedInterfaceData = new();

        /// <summary>
        /// Loaded on Init from serialized data.
        /// </summary>
        [DataField("interfaces")] internal List<PrototypeData> InterfaceData = new();
    }

    [DataDefinition]
    public sealed partial class PrototypeData
    {
        [DataField("key", required: true)]
        public Enum UiKey { get; private set; } = default!;

        [DataField("type", required: true)]
        public string ClientType { get; private set; } = default!;

        /// <summary>
        ///     Maximum range before a BUI auto-closes. A non-positive number means there is no limit.
        /// </summary>
        [DataField("range")]
        public float InteractionRange = 2f;

        // TODO BUI move to content?
        // I've tried to keep the name general, but really this is a bool for: can ghosts/stunned/dead people press buttons on this UI?
        /// <summary>
        ///     Determines whether the server should verify that a client is capable of performing generic UI interactions when receiving UI messages.
        /// </summary>
        /// <remarks>
        ///     Avoids requiring each system to individually validate client inputs. However, perhaps some BUIs are supposed to be bypass accessibility checks
        /// </remarks>
        [DataField("requireInputValidation")]
        public bool RequireInputValidation = true;
    }

    /// <summary>
    ///     Raised whenever the server receives a BUI message from a client relating to a UI that requires input
    ///     validation.
    /// </summary>
    public sealed class BoundUserInterfaceMessageAttempt : CancellableEntityEventArgs
    {
        public readonly ICommonSession Sender;
        public readonly EntityUid Target;
        public readonly Enum UiKey;

        public BoundUserInterfaceMessageAttempt(ICommonSession sender, EntityUid target, Enum uiKey)
        {
            Sender = sender;
            Target = target;
            UiKey = uiKey;
        }
    }

    [NetSerializable, Serializable]
    public abstract class BoundUserInterfaceState
    {
    }

    /// <summary>
    /// Abstract class for local BUI events.
    /// </summary>
    public abstract class BaseLocalBoundUserInterfaceEvent : BaseBoundUserInterfaceEvent
    {
        /// <summary>
        ///     The Entity receiving the message.
        ///     Only set when the message is raised as a directed event.
        /// </summary>
        public EntityUid Entity = EntityUid.Invalid;
    }

    /// <summary>
    /// Abstract class for all BUI events.
    /// </summary>
    [Serializable, NetSerializable]
    public abstract class BaseBoundUserInterfaceEvent : EntityEventArgs
    {
        /// <summary>
        ///     The UI of this message.
        ///     Only set when the message is raised as a directed event.
        /// </summary>
        public Enum UiKey = default!;

        /// <summary>
        ///     The session sending or receiving this message.
        ///     Only set when the message is raised as a directed event.
        /// </summary>
        [NonSerialized]
        public ICommonSession Session = default!;
    }

    /// <summary>
    /// Abstract class for networked BUI events.
    /// </summary>
    [NetSerializable, Serializable]
    public abstract class BoundUserInterfaceMessage : BaseBoundUserInterfaceEvent
    {
        /// <summary>
        ///     The Entity receiving the message.
        ///     Only set when the message is raised as a directed event.
        /// </summary>
        public NetEntity Entity { get; set; } = NetEntity.Invalid;
    }

    [NetSerializable, Serializable]
    internal sealed class UpdateBoundStateMessage : BoundUserInterfaceMessage
    {
        public readonly BoundUserInterfaceState State;

        public UpdateBoundStateMessage(BoundUserInterfaceState state)
        {
            State = state;
        }
    }

    [NetSerializable, Serializable]
    internal sealed class OpenBoundInterfaceMessage : BoundUserInterfaceMessage
    {
    }

    [NetSerializable, Serializable]
    internal sealed class CloseBoundInterfaceMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    internal abstract class BaseBoundUIWrapMessage : EntityEventArgs
    {
        public readonly NetEntity Entity;
        public readonly BoundUserInterfaceMessage Message;
        public readonly Enum UiKey;

        public BaseBoundUIWrapMessage(NetEntity entity, BoundUserInterfaceMessage message, Enum uiKey)
        {
            Message = message;
            UiKey = uiKey;
            Entity = entity;
        }
    }

    /// <summary>
    /// Helper message raised from client to server.
    /// </summary>
    [Serializable, NetSerializable]
    internal sealed class BoundUIWrapMessage : BaseBoundUIWrapMessage
    {
        public BoundUIWrapMessage(NetEntity entity, BoundUserInterfaceMessage message, Enum uiKey) : base(entity, message, uiKey)
        {
        }
    }

    /// <summary>
    /// Helper message raised from client to server.
    /// </summary>
    [Serializable, NetSerializable]
    internal sealed class PredictedBoundUIWrapMessage : BaseBoundUIWrapMessage
    {
        public PredictedBoundUIWrapMessage(NetEntity entity, BoundUserInterfaceMessage message, Enum uiKey) : base(entity, message, uiKey)
        {
        }
    }

    public sealed class BoundUIOpenedEvent : BaseLocalBoundUserInterfaceEvent
    {
        public BoundUIOpenedEvent(Enum uiKey, EntityUid uid, ICommonSession session)
        {
            UiKey = uiKey;
            Entity = uid;
            Session = session;
        }
    }

    public sealed class BoundUIClosedEvent : BaseLocalBoundUserInterfaceEvent
    {
        public BoundUIClosedEvent(Enum uiKey, EntityUid uid, ICommonSession session)
        {
            UiKey = uiKey;
            Entity = uid;
            Session = session;
        }
    }
}
