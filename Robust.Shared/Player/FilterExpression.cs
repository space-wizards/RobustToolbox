using System;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Player;

/// <summary>
/// Represents an approximation of a <see cref="Filter"/> that can be evaluated later against a different set of players.
/// </summary>
/// <remarks>
/// This is necessary for, for example, replays, which need to be able to filter stuff specially for replay cameras.
/// </remarks>
[Serializable, NetSerializable]
[DataDefinition]
internal sealed partial class FilterExpression
{
    /// <summary>
    /// Whether all players are included in the filter by default.
    /// </summary>
    [DataField]
    public bool DefaultAllPlayers;

    public bool Matches(ICommonSession session)
    {
        return DefaultAllPlayers;
    }
}
