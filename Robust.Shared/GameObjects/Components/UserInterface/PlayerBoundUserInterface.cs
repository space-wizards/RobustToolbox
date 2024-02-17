using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Player;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects;

/// <summary>
///     Represents an entity-bound interface that can be opened by multiple players at once.
/// </summary>
[PublicAPI]
public sealed class PlayerBoundUserInterface
{
    [ViewVariables]
    public float InteractionRange;

    [ViewVariables]
    public float InteractionRangeSqrd => InteractionRange * InteractionRange;

    [ViewVariables]
    public Enum UiKey { get; }
    [ViewVariables]
    public EntityUid Owner { get; }

    internal readonly HashSet<ICommonSession> _subscribedSessions = new();
    [ViewVariables]
    internal BoundUIWrapMessage? LastStateMsg;
    [ViewVariables(VVAccess.ReadWrite)]
    public bool RequireInputValidation;

    [ViewVariables]
    internal bool StateDirty;

    [ViewVariables]
    internal readonly Dictionary<ICommonSession, BoundUIWrapMessage> PlayerStateOverrides =
        new();

    /// <summary>
    ///     All of the sessions currently subscribed to this UserInterface.
    /// </summary>
    [ViewVariables]
    public IReadOnlySet<ICommonSession> SubscribedSessions => _subscribedSessions;

    public PlayerBoundUserInterface(PrototypeData data, EntityUid owner)
    {
        RequireInputValidation = data.RequireInputValidation;
        UiKey = data.UiKey;
        Owner = owner;

        InteractionRange = data.InteractionRange;
    }
}
