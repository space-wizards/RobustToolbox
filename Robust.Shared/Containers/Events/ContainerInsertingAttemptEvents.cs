using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers;

/// <summary>
/// Raised directed on the container when attempting to insert an entity.
/// </summary>
/// <remarks>
/// This event takes context into account and is called BEFORE uncontextualized event.
/// If you don't need to act on specific context, then use <see cref="ContainerIsInsertingAttemptEvent"/>.
/// </remarks>
/// <typeparam name="T">The type of the container action context.</typeparam>
[ByRefEvent]
public struct ContainerIsInsertingAttemptEvent<T>(T context, BaseContainer container, EntityUid entityUid, bool assumeEmpty)
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
    /// The entity that will be inserted into the container.
    /// </summary>
    public readonly EntityUid EntityUid = entityUid;

    /// <summary>
    /// If true, this check should assume that the container is currently empty.
    /// I.e., could the entity be inserted if the container doesn't contain anything else?
    /// </summary>
    public readonly bool AssumeEmpty = assumeEmpty;

    /// <summary>
    /// Whether to prevent the entity from being inserted.
    /// </summary>
    public bool Cancelled = false;
}

/// <summary>
/// Raised directed on the container when attempting to insert an entity.
/// </summary>
/// <remarks>
/// This event does not take context into account and is called AFTER contextual event.
/// If you need to act on specific context, then use <see cref="ContainerIsInsertingAttemptEvent{T}"/>.
/// </remarks>
[ByRefEvent]
public struct ContainerIsInsertingAttemptEvent(BaseContainer container, EntityUid entityUid, bool assumeEmpty)
{
    /// <summary>
    /// The container being acted upon.
    /// </summary>
    public readonly BaseContainer Container = container;

    /// <summary>
    /// The entity that will be inserted into the container.
    /// </summary>
    public readonly EntityUid EntityUid = entityUid;

    /// <summary>
    /// If true, this check should assume that the container is currently empty.
    /// I.e., could the entity be inserted if the container doesn't contain anything else?
    /// </summary>
    public readonly bool AssumeEmpty = assumeEmpty;

    /// <summary>
    /// Whether to prevent the entity from being inserted.
    /// </summary>
    public bool Cancelled = false;
}

/// <summary>
/// Raised directed on the entity being inserted into the container.
/// </summary>
/// <remarks>
/// This event takes context into account and is called BEFORE uncontextualized event.
/// If you don't need to act on specific context, then use <see cref="ContainerGettingInsertedAttemptEvent"/>.
/// </remarks>
/// <typeparam name="T">The type of the container action context.</typeparam>
[ByRefEvent]
public struct ContainerGettingInsertedAttemptEvent<T>(T context, BaseContainer container, EntityUid entityUid, bool assumeEmpty)
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
    /// The entity that will be inserted into the container.
    /// </summary>
    public readonly EntityUid EntityUid = entityUid;

    /// <summary>
    /// If true, this check should assume that the container is currently empty.
    /// I.e., could the entity be inserted if the container doesn't contain anything else?
    /// </summary>
    public readonly bool AssumeEmpty = assumeEmpty;

    /// <summary>
    /// Whether to prevent the entity from being inserted.
    /// </summary>
    public bool Cancelled = false;
}

/// <summary>
/// Raised directed on the entity being inserted into the container.
/// </summary>
/// <remarks>
/// This event does not take context into account and is called AFTER contextual event.
/// If you need to act on specific context, then use <see cref="ContainerGettingInsertedAttemptEvent{T}"/>.
/// </remarks>
[ByRefEvent]
public struct ContainerGettingInsertedAttemptEvent(BaseContainer container, EntityUid entityUid, bool assumeEmpty)
{
    /// <summary>
    /// The container being acted upon.
    /// </summary>
    public readonly BaseContainer Container = container;

    /// <summary>
    /// The entity that will be inserted into the container.
    /// </summary>
    public readonly EntityUid EntityUid = entityUid;

    /// <summary>
    /// If true, this check should assume that the container is currently empty.
    /// I.e., could the entity be inserted if the container doesn't contain anything else?
    /// </summary>
    public readonly bool AssumeEmpty = assumeEmpty;

    /// <summary>
    /// Whether to prevent the entity from being inserted.
    /// </summary>
    public bool Cancelled = false;
}
