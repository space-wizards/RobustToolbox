using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

/// <summary>
///     Represents an entity-bound interface that can be opened by multiple players at once.
/// </summary>
[DataDefinition, NetSerializable, Serializable]
public sealed partial class PlayerBoundUserInterface
{
    /// <summary>
    /// Datafield for this interaction range.
    /// </summary>
    [DataField]
    public float InteractionRange;

    public float InteractionRangeSqrd => InteractionRange * InteractionRange;

    /// <summary>
    /// UIKey to be used on the Owner BUI.
    /// </summary>
    [DataField]
    public Enum UiKey;

    /// <summary>
    /// Target entity for this BUI interaction.
    /// </summary>
    [DataField]
    public EntityUid Owner;

    internal readonly HashSet<ICommonSession> _subscribedSessions = new();

    /// <summary>
    /// Pending state message to be sent next tick.
    /// </summary>
    internal BoundUIWrapMessage? StateMessage;

    /// <summary>
    /// Is there a pending state message for next tick.
    /// </summary>
    internal bool StateDirty;

    [DataField, AutoNetworkedField]
    public bool RequireInputValidation;

    internal readonly Dictionary<ICommonSession, BoundUIWrapMessage> PlayerStateOverrides =
        new();

    /// <summary>
    ///     All of the sessions currently subscribed to this UserInterface.
    /// </summary>
    public IReadOnlySet<ICommonSession> SubscribedSessions => _subscribedSessions;

    public PlayerBoundUserInterface(PrototypeData data, EntityUid owner)
    {
        RequireInputValidation = data.RequireInputValidation;
        UiKey = data.UiKey;
        Owner = owner;

        InteractionRange = data.InteractionRange;
    }
}
