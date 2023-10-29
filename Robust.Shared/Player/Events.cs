using Robust.Shared.GameObjects;

namespace Robust.Shared.Player;

/// <summary>
/// Event that gets raised when a player has been attached to an entity. This event is both raised directed at the
/// entity and broadcast.
/// </summary>
public sealed class PlayerAttachedEvent : EntityEventArgs
{
    public readonly EntityUid Entity;
    public readonly ICommonSession Player;

    public PlayerAttachedEvent(EntityUid entity, ICommonSession player)
    {
        Entity = entity;
        Player = player;
    }
}

/// <summary>
/// Event that gets raised when a player has been detached from an entity. This event is both raised directed at the
/// entity and broadcast.
/// </summary>
public sealed class PlayerDetachedEvent : EntityEventArgs
{
    public readonly EntityUid Entity;
    public readonly ICommonSession Player;

    public PlayerDetachedEvent(EntityUid entity, ICommonSession player)
    {
        Entity = entity;
        Player = player;
    }
}

/// <summary>
/// Variant of <see cref="PlayerAttachedEvent"/> that gets raised by the client when the local session gets attached to
/// a new entity. This event will also get raised if the local session changes.
/// </summary>
public sealed class LocalPlayerAttachedEvent : EntityEventArgs
{
    public readonly EntityUid Entity;

    public LocalPlayerAttachedEvent(EntityUid entity)
    {
        Entity = entity;
    }
}

/// <summary>
/// Variant of <see cref="PlayerDetachedEvent"/> that gets raised by the client when the local session gets attached to
/// a new entity. This event will also get raised if the local session changes.
/// </summary>
public sealed class LocalPlayerDetachedEvent : EntityEventArgs
{
    public readonly EntityUid Entity;

    public LocalPlayerDetachedEvent(EntityUid entity)
    {
        Entity = entity;
    }
}
