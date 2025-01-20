using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;
using System;
using System.Numerics;

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
        var (uid, transform, meta, physics) = toInsert;

        // Cannot Use Resolve(ref toInsert) as the physics component is optional
        if (!Resolve(uid, ref transform, ref meta))
            return false;

        DebugTools.AssertOwner(container.Owner, containerXform);
        DebugTools.AssertOwner(toInsert, physics);
        DebugTools.Assert(!container.ExpectedEntities.Contains(GetNetEntity(toInsert)), "entity is expected");
        DebugTools.Assert(container.Manager.Containers.ContainsKey(container.ID), "manager does not own the container");

        // If someone is attempting to insert an entity into a container that is getting deleted, then we will
        // automatically delete that entity. I.e., the insertion automatically "succeeds" and both entities get deleted.
        // This is consistent with what happens if you attempt to attach an entity to a terminating parent.

        if (!TryComp(container.Owner, out MetaDataComponent? ownerMeta))
        {
            Log.Error($"Attempted to insert an entity {ToPrettyString(toInsert)} into a non-existent entity. Trace: {Environment.StackTrace}");
            QueueDel(toInsert);
            return false;
        }

        if (ownerMeta.EntityLifeStage >= EntityLifeStage.Terminating)
        {
            Log.Error($"Attempted to insert an entity {ToPrettyString(toInsert)} into an entity that is terminating. Entity: {ToPrettyString(container.Owner)}. Trace: {Environment.StackTrace}");
            QueueDel(toInsert);
            return false;
        }

        //Verify we can insert into this container
        if (!force && !CanInsert(uid, container, containerXform: containerXform))
            return false;

        if (meta.EntityLifeStage >= EntityLifeStage.Terminating)
        {
            Log.Error($"Attempted to insert a terminating entity {ToPrettyString(uid)} into a container {container.ID} in entity: {ToPrettyString(container.Owner)}. Trace: {Environment.StackTrace}");
            return false;
        }

        // remove from any old containers.
        if ((meta.Flags & MetaDataFlags.InContainer) != 0 &&
            TryComp(transform.ParentUid, out ContainerManagerComponent? oldManager) &&
            TryGetContainingContainer(transform.ParentUid, toInsert, out var oldContainer, oldManager) &&
            !Remove((uid, transform, meta), oldContainer, reparent: false, force: false))
        {
            // failed to remove from container --> cannot insert.
            return false;
        }

        // Update metadata first, so that parent change events can check IsInContainer.
        DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) == 0, "invalid metadata flags before insertion");
        meta.Flags |= MetaDataFlags.InContainer;

        // Remove the entity and any children from broadphases.
        // This is done before changing can collide to avoid unecceary updates.
        // TODO maybe combine with RecursivelyUpdatePhysics to avoid fetching components and iterating parents twice?
        _lookup.RemoveFromEntityTree(toInsert, transform);
        DebugTools.Assert(transform.Broadphase == null || !transform.Broadphase.Value.IsValid(), "invalid broadphase");

        // Avoid unnecessary broadphase updates while unanchoring, changing physics collision, and re-parenting.
        var old = transform.Broadphase;
        transform.Broadphase = BroadphaseData.Invalid;

        // Unanchor the entity (without changing physics body types).
        _transform.Unanchor(toInsert, transform, false);

        // Next, update physics. Note that this cannot just be done in the physics system via parent change events,
        // because the insertion may not result in a parent change. This could alternatively be done via a
        // got-inserted event, but really that event should run after the entity was actually inserted (so that
        // parent/map have updated). But we are better of disabling collision before doing map/parent changes.
        PhysicsQuery.Resolve(toInsert, ref physics, logMissing: false);
        RecursivelyUpdatePhysics((toInsert, transform, physics));

        // Attach to new parent
        var oldParent = transform.ParentUid;
        _transform.SetCoordinates(toInsert, transform, new EntityCoordinates(container.Owner, Vector2.Zero), Angle.Zero);
        transform.Broadphase = old;

        // the transform.AttachParent() could previously result in the flag being unset, so check that this hasn't happened.
        DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) != 0, "invalid metadata flags after insertion");

        // Implementation specific insert logic
        container.InternalInsert(toInsert, EntityManager);

        // Update any relevant joint relays
        // Can't be done above as the container flag isn't set yet.
        RecursivelyUpdateJoints((toInsert, transform));

        // Raise container events (after re-parenting and internal remove).
        RaiseLocalEvent(container.Owner, new EntInsertedIntoContainerMessage(toInsert, oldParent, container), true);
        RaiseLocalEvent(toInsert, new EntGotInsertedIntoContainerMessage(toInsert, container), true);

        // The sheer number of asserts tells you about how little I trust container and parenting code.
        DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) != 0, "invalid metadata flags after events");
        DebugTools.Assert(!transform.Anchored, "entity is anchored");
        DebugTools.AssertEqual(transform.LocalPosition, Vector2.Zero);
        DebugTools.Assert(MathHelper.CloseTo(transform.LocalRotation.Theta, Angle.Zero), "Angle is not zero");
        DebugTools.Assert(!PhysicsQuery.TryGetComponent(toInsert, out var phys) || (!phys.Awake && !phys.CanCollide), "Invalid physics");

        Dirty(container.Owner, container.Manager);
        return true;
    }

    /// <summary>
    /// Attempts to insert an entity into a container. If it fails, it will instead drop the entity next to the
    /// container entity.
    /// </summary>
    /// <returns>Whether or not the entity was successfully inserted</returns>
    public bool InsertOrDrop(Entity<TransformComponent?, MetaDataComponent?, PhysicsComponent?> toInsert,
        BaseContainer container,
        TransformComponent? containerXform = null)
    {
        if (!Resolve(toInsert.Owner, ref toInsert.Comp1) || !Resolve(container.Owner, ref containerXform))
            return false;

        if (Insert(toInsert, container, containerXform))
            return true;

        _transform.DropNextTo(toInsert, (container.Owner, containerXform));
        return false;
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

    private void RecursivelyUpdatePhysics(Entity<TransformComponent, PhysicsComponent?> entity)
    {
        if (entity.Comp2 is { } physics)
        {
            // TODO CONTAINER
            // Is this actually needed?
            // I.e., shouldn't this just do a if (_timing.ApplyingState) return

            // Here we intentionally don't dirty the physics comp. Client-side state handling will apply these same
            // changes. This also ensures that the server doesn't have to send the physics comp state to every
            // player for any entity inside of a container during init.
            _physics.SetLinearVelocity(entity, Vector2.Zero, false, body: physics);
            _physics.SetAngularVelocity(entity, 0, false, body: physics);
            _physics.SetCanCollide(entity, false, false, body: physics);
        }

        foreach (var child in entity.Comp1._children)
        {
            var childXform = TransformQuery.GetComponent(child);
            PhysicsQuery.TryGetComponent(child, out var childPhysics);
            RecursivelyUpdatePhysics((child, childXform, childPhysics));
        }
    }

    internal void RecursivelyUpdateJoints(Entity<TransformComponent> entity)
    {
        if (JointQuery.TryGetComponent(entity, out var jointComp))
        {
            // TODO: This is going to be going up while joints going down, although these aren't too common
            // in SS14 atm.
            _joint.RefreshRelay(entity, jointComp);
        }

        foreach (var child in entity.Comp._children)
        {
            var childXform = TransformQuery.GetComponent(child);
            RecursivelyUpdateJoints((child, childXform));
        }
    }
}
