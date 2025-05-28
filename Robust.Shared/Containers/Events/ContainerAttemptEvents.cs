using System.ComponentModel;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers;

public abstract class ContainerAttemptEventBase : CancellableEntityEventArgs
{
    public readonly BaseContainer Container;
    public readonly EntityUid EntityUid;

    public ContainerAttemptEventBase(BaseContainer container, EntityUid entityUid)
    {
        Container = container;
        EntityUid = entityUid;
    }
}

/// <summary>
/// Raised directed on the container when attempting to insert an entity.
/// </summary>
public sealed class ContainerIsInsertingAttemptEvent : ContainerAttemptEventBase
{
    /// <summary>
    /// If true, this check should assume that the container is currently empty.
    /// I.e., could the entity be inserted if the container doesn't contain anything else?
    /// </summary>
    public bool AssumeEmpty { get; set; }

    public ContainerIsInsertingAttemptEvent(BaseContainer container, EntityUid entityUid, bool assumeEmpty)
        : base(container, entityUid)
    {
        AssumeEmpty = assumeEmpty;
    }
}

/// <summary>
/// Raised directed on the entity being inserted into the container.
/// </summary>
public sealed class ContainerGettingInsertedAttemptEvent : ContainerAttemptEventBase
{
    /// <summary>
    /// If true, this check should assume that the container is currently empty.
    /// I.e., could the entity be inserted if the container doesn't contain anything else?
    /// </summary>
    public bool AssumeEmpty { get; set; }

    public ContainerGettingInsertedAttemptEvent(BaseContainer container, EntityUid entityUid, bool assumeEmpty)
        : base(container, entityUid)
    {
        AssumeEmpty = assumeEmpty;
    }
}

/// <summary>
/// Raised directed on the container when attempting to remove an entity.
/// </summary>
public sealed class ContainerIsRemovingAttemptEvent : ContainerAttemptEventBase
{
    public ContainerIsRemovingAttemptEvent(BaseContainer container, EntityUid entityUid) : base(container, entityUid)
    {
    }
}

/// <summary>
/// Raised directed on the entity being removed from the container.
/// </summary>
public sealed class ContainerGettingRemovedAttemptEvent : ContainerAttemptEventBase
{
    public ContainerGettingRemovedAttemptEvent(BaseContainer container, EntityUid entityUid) : base(container, entityUid)
    {
    }
}
