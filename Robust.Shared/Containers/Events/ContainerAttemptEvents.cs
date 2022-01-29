using System.ComponentModel;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers;

public abstract class ContainerAttemptEventBase : CancellableEntityEventArgs
{
    public readonly IContainer Container;
    public readonly EntityUid EntityUid;

    public ContainerAttemptEventBase(IContainer container, EntityUid entityUid)
    {
        Container = container;
        EntityUid = entityUid;
    }
}

public sealed class ContainerIsInsertingAttemptEvent : ContainerAttemptEventBase
{
    public ContainerIsInsertingAttemptEvent(IContainer container, EntityUid entityUid) : base(container, entityUid)
    {
    }
}

public sealed class ContainerGettingInsertedAttemptEvent : ContainerAttemptEventBase
{
    public ContainerGettingInsertedAttemptEvent(IContainer container, EntityUid entityUid) : base(container, entityUid)
    {
    }
}

public sealed class ContainerIsRemovingAttemptEvent : ContainerAttemptEventBase
{
    public ContainerIsRemovingAttemptEvent(IContainer container, EntityUid entityUid) : base(container, entityUid)
    {
    }
}

public sealed class ContainerGettingRemovedAttemptEvent : ContainerAttemptEventBase
{
    public ContainerGettingRemovedAttemptEvent(IContainer container, EntityUid entityUid) : base(container, entityUid)
    {
    }
}
