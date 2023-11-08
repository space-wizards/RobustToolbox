using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Containers;

public abstract partial class SharedContainerSystem
{

    /// <summary>
    /// Attempts to insert the entity into this container.
    /// </summary>
    /// <remarks>
    /// If the insertion is successful, the inserted entity will end up parented to the
    /// container entity, and the inserted entity's local position will be set to the zero vector.
    /// </remarks>
    /// <param name="toInsert">The entity to insert.</param>
    /// <param name="container">The container to insert into.</param>
    /// <param name="containerXform">The container's transform component.</param>
    /// <param name="force">Whether to bypass normal insertion checks.</param>
    /// <returns>False if the entity could not be inserted.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this container is a child of the entity,
    /// which would cause infinite loops.
    /// </exception>
    public bool Insert(Entity<TransformComponent?, MetaDataComponent?, PhysicsComponent?> toInsert,
        BaseContainer container,
        TransformComponent? containerXform = null,
        bool force = false)
    {
        // Cannot Use Resolve(ref toInsert) as the physics component is optional
        if (!Resolve(toInsert.Owner, ref toInsert.Comp1, ref toInsert.Comp2))
            return false;

        // TODO move logic over to the system.
        return container.Insert(toInsert, EntityManager, toInsert, containerXform, toInsert, toInsert, force);
    }

    /// <summary>
    /// Checks if the entity can be inserted into the given container.
    /// </summary>
    /// <param name="assumeEmpty">If true, this will check whether the entity could be inserted if the container were
    /// empty.</param>
    public bool CanInsert(
        EntityUid toInsert,
        BaseContainer container,
        bool assumeEmpty = false,
        TransformComponent? containerXform = null)
    {
        if (container.Owner == toInsert)
            return false;

        if (!assumeEmpty && container.Contains(toInsert))
            return false;

        if (!container.CanInsert(toInsert, assumeEmpty, EntityManager))
            return false;

        // no, you can't put maps or grids into containers
        if (_mapQuery.HasComponent(toInsert) || _gridQuery.HasComponent(toInsert))
            return false;

        // Prevent circular insertion.
        if (_transform.ContainsEntity(toInsert, (container.Owner, containerXform)))
            return false;

        var insertAttemptEvent = new ContainerIsInsertingAttemptEvent(container, toInsert, assumeEmpty);
        RaiseLocalEvent(container.Owner, insertAttemptEvent, true);
        if (insertAttemptEvent.Cancelled)
            return false;

        var gettingInsertedAttemptEvent = new ContainerGettingInsertedAttemptEvent(container, toInsert, assumeEmpty);
        RaiseLocalEvent(toInsert, gettingInsertedAttemptEvent, true);

        return !gettingInsertedAttemptEvent.Cancelled;
    }
}
