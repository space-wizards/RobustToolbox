using Robust.Shared.Players;

namespace Robust.Shared.GameObjects;

/// <summary>
///     Event for when a player has been detached from an entity.
/// </summary>
[ByRefEvent]
public readonly record struct PlayerDetachedEvent(EntityUid Entity, ICommonSession Player)
{
    public readonly EntityUid Entity = Entity;
    public readonly ICommonSession Player = Player;
}
