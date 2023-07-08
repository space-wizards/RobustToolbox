using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;
using static Robust.Shared.GameObjects.SharedUserInterfaceComponent;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     Contains a collection of entity-bound user interfaces that can be opened per client.
    ///     Bound user interfaces are indexed with an enum or string key identifier.
    /// </summary>
    /// <seealso cref="BoundUserInterface"/>
    [PublicAPI]
    [RegisterComponent, ComponentReference(typeof(SharedUserInterfaceComponent))]
    public sealed class ServerUserInterfaceComponent : SharedUserInterfaceComponent
    {
        [ViewVariables]
        public readonly Dictionary<Enum, BoundUserInterface> Interfaces = new();
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
        public EntityUid Owner { get; }

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

        public BoundUserInterface(PrototypeData data, EntityUid owner)
        {
            RequireInputValidation = data.RequireInputValidation;
            UiKey = data.UiKey;
            Owner = owner;

            // One Abs(), because negative values imply no limit
            InteractionRangeSqrd = data.InteractionRange * MathF.Abs(data.InteractionRange);
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
