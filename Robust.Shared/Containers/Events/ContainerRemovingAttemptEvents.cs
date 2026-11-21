using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers;

/// <summary>
/// Raised directed on the container when attempting to remove an entity.
/// </summary>
/// <remarks>
/// This event takes context into account and is called BEFORE uncontextualized event.
/// If you don't need to act on specific context, then use <see cref="ContainerIsRemovingAttemptEvent"/>.
/// </remarks>
/// <typeparam name="T">The type of the container action context.</typeparam>
[ByRefEvent]
public struct ContainerIsRemovingAttemptEvent<T>(T context, BaseContainer container, EntityUid entityUid)
    where T : struct, IContainerActionContext
{
    /// <summary>
    /// Context explaining why or how this container action occurred.
    /// I.e., the actor taking the item out of the container (<see cref="ContainerActorContext"/>).
    /// </summary>
    public readonly T Context = context;

    /// <summary>
    /// The container being acted upon.
    /// </summary>
    public readonly BaseContainer Container = container;

    /// <summary>
    /// The entity that will be removed from the container.
    /// </summary>
    public readonly EntityUid EntityUid = entityUid;

    /// <summary>
    /// Whether to prevent the entity from being removed.
    /// </summary>
    public bool Cancelled = false;
}

/// <summary>
/// Raised directed on the container when attempting to remove an entity.
/// </summary>
/// <remarks>
/// This event does not take context into account and is called AFTER contextual event.
/// If you need to act on specific context, then use <see cref="ContainerIsRemovingAttemptEvent{T}"/>.
/// </remarks>
[ByRefEvent]
public struct ContainerIsRemovingAttemptEvent(BaseContainer container, EntityUid entityUid)
{
    /// <summary>
    /// The container being acted upon.
    /// </summary>
    public readonly BaseContainer Container = container;

    /// <summary>
    /// The entity that will be removed from the container.
    /// </summary>
    public readonly EntityUid EntityUid = entityUid;

    /// <summary>
    /// Whether to prevent the entity from being removed.
    /// </summary>
    public bool Cancelled = false;
}

/// <summary>
/// Raised directed on the entity being removed from the container.
/// </summary>
/// <remarks>
/// This event takes context into account and is called BEFORE uncontextualized event.
/// If you don't need to act on specific context, then use <see cref="ContainerGettingRemovedAttemptEvent"/>.
/// </remarks>
/// <typeparam name="T">The type of the container action context.</typeparam>
[ByRefEvent]
public struct ContainerGettingRemovedAttemptEvent<T>(T context, BaseContainer container, EntityUid entityUid)
    where T : struct, IContainerActionContext
{
    /// <summary>
    /// Context explaining why or how this container action occurred.
    /// I.e., the actor taking the item out of the container (<see cref="ContainerActorContext"/>).
    /// </summary>
    public readonly T Context = context;

    /// <summary>
    /// The container being acted upon.
    /// </summary>
    public readonly BaseContainer Container = container;

    /// <summary>
    /// The entity that will be removed from the container.
    /// </summary>
    public readonly EntityUid EntityUid = entityUid;

    /// <summary>
    /// Whether to prevent the entity from being removed.
    /// </summary>
    public bool Cancelled = false;
}

/// <summary>
/// Raised directed on the entity being removed from the container.
/// </summary>
/// <remarks>
/// This event does not take context into account and is called AFTER contextual event.
/// If you need to act on specific context, then use <see cref="ContainerGettingRemovedAttemptEvent{T}"/>.
/// </remarks>
[ByRefEvent]
public struct ContainerGettingRemovedAttemptEvent(BaseContainer container, EntityUid entityUid)
{
    /// <summary>
    /// The container being acted upon.
    /// </summary>
    public readonly BaseContainer Container = container;

    /// <summary>
    /// The entity that will be removed from the container.
    /// </summary>
    public readonly EntityUid EntityUid = entityUid;

    /// <summary>
    /// Whether to prevent the entity from being removed.
    /// </summary>
    public bool Cancelled = false;
}
