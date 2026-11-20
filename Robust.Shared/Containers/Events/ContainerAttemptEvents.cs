using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers;

public abstract class ContainerAttemptEventBase(BaseContainer container, EntityUid entityUid, ContainerActionContext? context = null) :
    CancellableEntityEventArgs
{
    /// <summary>
    /// The container being acted upon.
    /// </summary>
    public readonly BaseContainer Container = container;

    /// <summary>
    /// The entity that was removed or inserted from/into the container.
    /// </summary>
    public readonly EntityUid EntityUid = entityUid;

    /// <summary>
    /// Optional context explaining why or how this container action occurred.
    /// I.e., the actor taking the item out of the container (<see cref="ContainerActorContext"/>).
    /// </summary>
    public readonly ContainerActionContext? Context = context;
}

/// <summary>
/// Raised directed on the container when attempting to insert an entity.
/// </summary>
public sealed class ContainerIsInsertingAttemptEvent(BaseContainer container, EntityUid entityUid, bool assumeEmpty, ContainerActionContext? context = null) :
    ContainerAttemptEventBase(container, entityUid, context)
{
    /// <summary>
    /// If true, this check should assume that the container is currently empty.
    /// I.e., could the entity be inserted if the container doesn't contain anything else?
    /// </summary>
    public bool AssumeEmpty { get; set; } = assumeEmpty;
}

/// <summary>
/// Raised directed on the entity being inserted into the container.
/// </summary>
public sealed class ContainerGettingInsertedAttemptEvent(BaseContainer container, EntityUid entityUid, bool assumeEmpty, ContainerActionContext? context = null) :
    ContainerAttemptEventBase(container, entityUid, context)
{
    /// <summary>
    /// If true, this check should assume that the container is currently empty.
    /// I.e., could the entity be inserted if the container doesn't contain anything else?
    /// </summary>
    public bool AssumeEmpty { get; set; } = assumeEmpty;
}

/// <summary>
/// Raised directed on the container when attempting to remove an entity.
/// </summary>
public sealed class ContainerIsRemovingAttemptEvent(BaseContainer container, EntityUid entityUid, ContainerActionContext? context = null) :
    ContainerAttemptEventBase(container, entityUid, context);

/// <summary>
/// Raised directed on the entity being removed from the container.
/// </summary>
public sealed class ContainerGettingRemovedAttemptEvent(BaseContainer container, EntityUid entityUid, ContainerActionContext? context = null) :
    ContainerAttemptEventBase(container, entityUid, context);
