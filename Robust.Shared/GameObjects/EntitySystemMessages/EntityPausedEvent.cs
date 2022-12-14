using System;

namespace Robust.Shared.GameObjects;

/// <summary>
/// Raised directed on an entity when it is paused.
/// </summary>
[ByRefEvent]
public readonly record struct EntityPausedEvent;

/// <summary>
/// Raised directed on an entity when it is unpaused.
/// </summary>
[ByRefEvent]
public readonly record struct EntityUnpausedEvent(TimeSpan PausedTime)
{
    public readonly TimeSpan PausedTime = PausedTime;
}
