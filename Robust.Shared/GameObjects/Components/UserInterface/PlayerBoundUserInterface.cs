using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Robust.Shared.GameObjects;

/// <summary>
///     Represents an entity-bound interface that can be opened by multiple players at once.
/// </summary>
[PublicAPI]
public sealed class PlayerBoundUserInterface
{
    public float InteractionRange;

    public float InteractionRangeSqrd => InteractionRange * InteractionRange;

    public Enum UiKey { get; }
    public EntityUid Owner { get; }

    internal readonly HashSet<ICommonSession> _subscribedSessions = new();
    internal BoundUIWrapMessage? LastStateMsg;
    public bool RequireInputValidation;

    internal bool StateDirty;

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
