using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent, NetworkedComponent, Access(typeof(SharedUserInterfaceSystem))]
    public sealed partial class UserInterfaceComponent : Component
    {
        /// <summary>
        /// The currently open interfaces. Used clientside to store the UI.
        /// </summary>
        [ViewVariables, Access(Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.ReadWriteExecute)]
        public readonly Dictionary<Enum, BoundUserInterface> ClientOpenInterfaces = new();

        [DataField]
        internal Dictionary<Enum, InterfaceData> Interfaces = new();

        /// <summary>
        /// Actors that currently have interfaces open.
        /// </summary>
        [DataField]
        public Dictionary<Enum, HashSet<EntityUid>> Actors = new();

        /// <summary>
        /// Legacy data, new BUIs should be using comp states.
        /// </summary>
        public Dictionary<Enum, BoundUserInterfaceState> States = new();

        [Serializable, NetSerializable]
        internal sealed class UserInterfaceComponentState(
            Dictionary<Enum, List<NetEntity>> actors,
            Dictionary<Enum, BoundUserInterfaceState> states)
            : IComponentState
        {
            public Dictionary<Enum, List<NetEntity>> Actors = actors;

            public Dictionary<Enum, BoundUserInterfaceState> States = states;
        }
    }

    [DataDefinition]
    public sealed partial class InterfaceData
    {
        [DataField("type", required: true)]
        public string ClientType { get; private set; } = default!;

        /// <summary>
        ///     Maximum range before a BUI auto-closes. A non-positive number means there is no limit.
        /// </summary>
        [DataField]
        public float InteractionRange = 2f;

        // TODO BUI move to content?
        // I've tried to keep the name general, but really this is a bool for: can ghosts/stunned/dead people press buttons on this UI?
        /// <summary>
        ///     Determines whether the server should verify that a client is capable of performing generic UI interactions when receiving UI messages.
        /// </summary>
        /// <remarks>
        ///     Avoids requiring each system to individually validate client inputs. However, perhaps some BUIs are supposed to be bypass accessibility checks
        /// </remarks>
        [DataField]
        public bool RequireInputValidation = true;
    }

    /// <summary>
    ///     Raised whenever the server receives a BUI message from a client relating to a UI that requires input
    ///     validation.
    /// </summary>
    public sealed class BoundUserInterfaceMessageAttempt(
        EntityUid actor,
        EntityUid target,
        Enum uiKey,
        BoundUserInterfaceMessage message)
        : CancellableEntityEventArgs
    {
        public readonly EntityUid Actor = actor;
        public readonly EntityUid Target = target;
        public readonly Enum UiKey = uiKey;
        public readonly BoundUserInterfaceMessage Message = message;
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
        public EntityUid Actor = default!;
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
    public sealed class OpenBoundInterfaceMessage : BoundUserInterfaceMessage
    {
    }

    [NetSerializable, Serializable]
    public sealed class CloseBoundInterfaceMessage : BoundUserInterfaceMessage
    {
    }

    [Serializable, NetSerializable]
    internal abstract class BaseBoundUIWrapMessage(NetEntity entity, BoundUserInterfaceMessage message, Enum uiKey)
        : EntityEventArgs
    {
        public readonly NetEntity Entity = entity;
        public readonly BoundUserInterfaceMessage Message = message;
        public readonly Enum UiKey = uiKey;
    }

    /// <summary>
    /// Helper message raised from client to server.
    /// </summary>
    [Serializable, NetSerializable]
    internal sealed class BoundUIWrapMessage(NetEntity entity, BoundUserInterfaceMessage message, Enum uiKey)
        : BaseBoundUIWrapMessage(entity, message, uiKey);

    public sealed class BoundUIOpenedEvent : BaseLocalBoundUserInterfaceEvent
    {
        public BoundUIOpenedEvent(Enum uiKey, EntityUid uid, EntityUid actor)
        {
            UiKey = uiKey;
            Entity = uid;
            Actor = actor;
        }
    }

    public sealed class BoundUIClosedEvent : BaseLocalBoundUserInterfaceEvent
    {
        public BoundUIClosedEvent(Enum uiKey, EntityUid uid, EntityUid actor)
        {
            UiKey = uiKey;
            Entity = uid;
            Actor = actor;
        }
    }
}
