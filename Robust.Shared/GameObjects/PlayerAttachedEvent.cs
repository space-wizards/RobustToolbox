using Robust.Shared.Players;

namespace Robust.Shared.GameObjects;

/// <summary>
///     Event for when a player has been attached to an entity.
/// </summary>
[ByRefEvent]
public readonly record struct PlayerAttachedEvent(EntityUid Entity, ICommonSession Player, ICommonSession? Kicked = null)
{
    public readonly EntityUid Entity = Entity;
    public readonly ICommonSession Player = Player;

    /// <summary>
    ///     The player session that was forcefully kicked from the entity, if any.
    /// </summary>
    public readonly ICommonSession? Kicked = Kicked;
}
