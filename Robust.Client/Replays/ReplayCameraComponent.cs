using Robust.Shared.GameObjects;

namespace Robust.Client.Replays;

/// <summary>
/// This entity is being spectated through a replay, and may or may not have existed in the original replay.
/// </summary>
[RegisterComponent]
public sealed partial class ReplayCameraComponent : Component
{
    /// <summary>
    /// If true, this entity is an actor in the replay before being spectated.
    /// </summary>
    /// <remarks>
    /// This would be true if spectating an active player,
    /// but false for a free-flying replay camera or a mob that was not player-controlled.
    /// </remarks>
    public bool IsActorInReplay;
}
