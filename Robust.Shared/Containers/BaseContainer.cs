using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using Robust.Shared.Map.Components;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Base container class that all container inherit from.
    /// </summary>
    public abstract class BaseContainer : IContainer
    {
        /// <inheritdoc />
        [ViewVariables]
        public abstract IReadOnlyList<EntityUid> ContainedEntities { get; }

        [ViewVariables]
        public abstract List<EntityUid> ExpectedEntities { get; }

        /// <inheritdoc />
        public abstract string ContainerType { get; }

        /// <inheritdoc />
        [ViewVariables]
        public bool Deleted { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public string ID { get; internal set; } = default!; // Make sure you set me in init

        /// <inheritdoc />
        public ContainerManagerComponent Manager { get; internal set; } = default!; // Make sure you set me in init

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("occludes")]
        public bool OccludesLight { get; set; } = true;

        /// <inheritdoc />
        [ViewVariables]
        public EntityUid Owner => Manager.Owner;

        /// <inheritdoc />
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("showEnts")]
        public bool ShowContents { get; set; }

        /// <summary>
        /// DO NOT CALL THIS METHOD DIRECTLY!
        /// You want <see cref="IContainerManager.MakeContainer{T}(string)" /> instead.
        /// </summary>
        protected BaseContainer() { }

        /// <inheritdoc />
        public bool Insert(
            EntityUid toinsert,
            IEntityManager? entMan = null,
            TransformComponent? transform = null,
            TransformComponent? ownerTransform = null,
            MetaDataComponent? meta = null,
            PhysicsComponent? physics = null)
        {
            DebugTools.Assert(!Deleted);
            DebugTools.Assert(transform == null || transform.Owner == toinsert);
            DebugTools.Assert(ownerTransform == null || ownerTransform.Owner == Owner);
            DebugTools.Assert(ownerTransform == null || ownerTransform.Owner == Owner);
            DebugTools.Assert(physics == null || physics.Owner == toinsert);
            DebugTools.Assert(!ExpectedEntities.Contains(toinsert));
            IoCManager.Resolve(ref entMan);

            //Verify we can insert into this container
            if (!CanInsert(toinsert, entMan))
                return false;

            var physicsQuery = entMan.GetEntityQuery<PhysicsComponent>();
            var transformQuery = entMan.GetEntityQuery<TransformComponent>();
            var jointQuery = entMan.GetEntityQuery<JointComponent>();

            // ECS containers when
            var physicsSys = entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>();
            var jointSys = entMan.EntitySysManager.GetEntitySystem<SharedJointSystem>();

            // Please somebody ecs containers
            var lookupSys = entMan.EntitySysManager.GetEntitySystem<EntityLookupSystem>();
            var xformSys = entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>();

            transform ??= transformQuery.GetComponent(toinsert);
            meta ??= entMan.GetComponent<MetaDataComponent>(toinsert);

            // remove from any old containers.
            if ((meta.Flags & MetaDataFlags.InContainer) != 0 &&
                entMan.TryGetComponent(transform.ParentUid, out ContainerManagerComponent? oldManager) &&
                oldManager.TryGetContainer(toinsert, out var oldContainer) &&
                !oldContainer.Remove(toinsert, entMan, transform, meta, false, false))
            {
                // failed to remove from container --> cannot insert.
                return false;
            }

            // Update metadata first, so that parent change events can check IsInContainer.
            DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) == 0);
            meta.Flags |= MetaDataFlags.InContainer;

            // Remove the entity and any children from broadphases.
            // This is done before changing can collide to avoid unecceary updates.
            // TODO maybe combine with RecursivelyUpdatePhysics to avoid fetching components and iterating parents twice?
            lookupSys.RemoveFromEntityTree(toinsert, transform, transformQuery);
            DebugTools.Assert(transform.Broadphase == null || !transform.Broadphase.Value.IsValid());

            // Avoid unnecessary broadphase updates while unanchoring, changing physics collision, and re-parenting.
            var old = transform.Broadphase;
            transform.Broadphase = BroadphaseData.Invalid;

            // Unanchor the entity (without changing physics body types).
            xformSys.Unanchor(transform, false);

            // Next, update physics. Note that this cannot just be done in the physics system via parent change events,
            // because the insertion may not result in a parent change. This could alternatively be done via a
            // got-inserted event, but really that event should run after the entity was actually inserted (so that
            // parent/map have updated). But we are better of disabling collision before doing map/parent changes.
            physicsQuery.Resolve(toinsert, ref physics, false);
            RecursivelyUpdatePhysics(transform, physics, physicsSys, jointSys, physicsQuery, transformQuery, jointQuery);

            // Attach to new parent
            var oldParent = transform.ParentUid;
            xformSys.SetCoordinates(transform, new EntityCoordinates(Owner, Vector2.Zero), Angle.Zero);
            transform.Broadphase = old;

            // the transform.AttachParent() could previously result in the flag being unset, so check that this hasn't happened.
            DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) != 0);

            // Implementation specific insert logic
            InternalInsert(toinsert, entMan);

            // Raise container events (after re-parenting and internal remove).
            entMan.EventBus.RaiseLocalEvent(Owner, new EntInsertedIntoContainerMessage(toinsert, oldParent, this), true);
            entMan.EventBus.RaiseLocalEvent(toinsert, new EntGotInsertedIntoContainerMessage(toinsert, this), true);

            // The sheer number of asserts tells you about how little I trust container and parenting code.
            DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) != 0);
            DebugTools.Assert(!transform.Anchored);
            DebugTools.Assert(transform.LocalPosition == Vector2.Zero);
            DebugTools.Assert(transform.LocalRotation == Angle.Zero);
            DebugTools.Assert(!physicsQuery.TryGetComponent(toinsert, out var phys) || (!phys.Awake && !phys.CanCollide));

            entMan.Dirty(Manager);
            return true;
        }

        internal void RecursivelyUpdatePhysics(TransformComponent xform,
            PhysicsComponent? physics,
            SharedPhysicsSystem physicsSys,
            SharedJointSystem jointSys,
            EntityQuery<PhysicsComponent> physicsQuery,
            EntityQuery<TransformComponent> transformQuery,
            EntityQuery<JointComponent> jointQuery)
        {
            if (physics != null)
            {
                // Here we intentionally don't dirty the physics comp. Client-side state handling will apply these same
                // changes. This also ensures that the server doesn't have to send the physics comp state to every
                // player for any entity inside of a container during init.
                physicsSys.SetLinearVelocity(physics, Vector2.Zero, false);
                physicsSys.SetAngularVelocity(physics, 0, false);
                physicsSys.SetCanCollide(physics, false, false);

                if (jointQuery.TryGetComponent(xform.Owner, out var joint))
                    jointSys.ClearJoints(xform.Owner, joint);
            }

            foreach (var child in xform.ChildEntities)
            {
                var childXform = transformQuery.GetComponent(child);
                physicsQuery.TryGetComponent(child, out var childPhysics);
                RecursivelyUpdatePhysics(childXform, childPhysics, physicsSys, jointSys, physicsQuery, transformQuery, jointQuery);
            }
        }

        /// <inheritdoc />
        public virtual bool CanInsert(EntityUid toinsert, IEntityManager? entMan = null)
        {
            DebugTools.Assert(!Deleted);

            // cannot insert into itself.
            if (Owner == toinsert)
                return false;

            IoCManager.Resolve(ref entMan);

            // no, you can't put maps or grids into containers
            if (entMan.HasComponent<MapComponent>(toinsert) || entMan.HasComponent<MapGridComponent>(toinsert))
                return false;

            var xformSystem = entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
            var xformQuery = entMan.GetEntityQuery<TransformComponent>();

            // Crucial, prevent circular insertion.
            if (xformSystem.ContainsEntity(xformQuery.GetComponent(toinsert), Owner, xformQuery))
                return false;

            //Improvement: Traverse the entire tree to make sure we are not creating a loop.

            //raise events
            var insertAttemptEvent = new ContainerIsInsertingAttemptEvent(this, toinsert);
            entMan.EventBus.RaiseLocalEvent(Owner, insertAttemptEvent, true);
            if (insertAttemptEvent.Cancelled)
                return false;

            var gettingInsertedAttemptEvent = new ContainerGettingInsertedAttemptEvent(this, toinsert);
            entMan.EventBus.RaiseLocalEvent(toinsert, gettingInsertedAttemptEvent, true);
            if (gettingInsertedAttemptEvent.Cancelled)
                return false;

            return true;
        }

        /// <inheritdoc />
        public bool Remove(
            EntityUid toRemove,
            IEntityManager? entMan = null,
            TransformComponent? xform = null,
            MetaDataComponent? meta = null,
            bool reparent = true,
            bool force = false,
            EntityCoordinates? destination = null,
            Angle? localRotation = null)
        {
            IoCManager.Resolve(ref entMan);
            DebugTools.Assert(!Deleted);
            DebugTools.AssertNotNull(Manager);
            DebugTools.Assert(entMan.EntityExists(toRemove));
            DebugTools.Assert(xform == null || xform.Owner == toRemove);
            DebugTools.Assert(meta == null || meta.Owner == toRemove);

            xform ??= entMan.GetComponent<TransformComponent>(toRemove);
            meta ??= entMan.GetComponent<MetaDataComponent>(toRemove);

            if (!force && !CanRemove(toRemove, entMan))
                return false;

            if (force && !Contains(toRemove))
            {
                DebugTools.Assert("Attempted to force remove an entity that was never inside of the container.");
                return false;
            }

            DebugTools.Assert(meta.EntityLifeStage < EntityLifeStage.Terminating || (force && !reparent));
            DebugTools.Assert(xform.Broadphase == null || !xform.Broadphase.Value.IsValid());
            DebugTools.Assert(!xform.Anchored);
            DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) != 0x0);
            DebugTools.Assert(!entMan.TryGetComponent(toRemove, out PhysicsComponent? phys) || (!phys.Awake && !phys.CanCollide));

            // Unset flag (before parent change events are raised).
            meta.Flags &= ~MetaDataFlags.InContainer;

            // Implementation specific remove logic
            InternalRemove(toRemove, entMan);

            DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) == 0x0);
            var oldParent = xform.ParentUid;

            if (destination != null)
            {
                // Container ECS when.
                entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().SetCoordinates(xform, destination.Value, localRotation);
            }
            else if (reparent)
            {
                // Container ECS when.
                entMan.EntitySysManager.GetEntitySystem<SharedContainerSystem>().AttachParentToContainerOrGrid(xform);
                if (localRotation != null)
                    entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().SetLocalRotation(xform, localRotation.Value);
            }

            // Add to new broadphase
            if (xform.ParentUid == oldParent // move event should already have handled it
                && xform.Broadphase == null) // broadphase explicitly invalid?
            {
                entMan.EntitySysManager.GetEntitySystem<EntityLookupSystem>().FindAndAddToEntityTree(toRemove, xform);
            }

            // Raise container events (after re-parenting and internal remove).
            entMan.EventBus.RaiseLocalEvent(Owner, new EntRemovedFromContainerMessage(toRemove, this), true);
            entMan.EventBus.RaiseLocalEvent(toRemove, new EntGotRemovedFromContainerMessage(toRemove, this), false);

            DebugTools.Assert(destination == null || xform.Coordinates.Equals(destination.Value));

            entMan.Dirty(Manager);
            return true;
        }

        [Obsolete("use force option in Remove()")]
        public void ForceRemove(EntityUid toRemove, IEntityManager? entMan = null, MetaDataComponent? meta = null)
            => Remove(toRemove, entMan, meta: meta, reparent: false, force: true);

        /// <inheritdoc />
        public virtual bool CanRemove(EntityUid toRemove, IEntityManager? entMan = null)
        {
            DebugTools.Assert(!Deleted);

            if (!Contains(toRemove))
                return false;

            IoCManager.Resolve(ref entMan);

            //raise events
            var removeAttemptEvent = new ContainerIsRemovingAttemptEvent(this, toRemove);
            entMan.EventBus.RaiseLocalEvent(Owner, removeAttemptEvent, true);
            if (removeAttemptEvent.Cancelled)
                return false;

            var gettingRemovedAttemptEvent = new ContainerGettingRemovedAttemptEvent(this, toRemove);
            entMan.EventBus.RaiseLocalEvent(toRemove, gettingRemovedAttemptEvent, true);
            if (gettingRemovedAttemptEvent.Cancelled)
                return false;

            return true;
        }

        /// <inheritdoc />
        public abstract bool Contains(EntityUid contained);

        /// <inheritdoc />
        public void Shutdown(IEntityManager? entMan = null, INetManager? netMan = null)
        {
            IoCManager.Resolve(ref entMan, ref netMan);
            InternalShutdown(entMan, netMan.IsClient);
            Manager.Containers.Remove(ID);
            Deleted = true;
        }

        /// <inheritdoc />
        protected abstract void InternalShutdown(IEntityManager entMan, bool isClient);

        /// <summary>
        /// Implement to store the reference in whatever form you want
        /// </summary>
        /// <param name="toInsert"></param>
        /// <param name="entMan"></param>
        protected abstract void InternalInsert(EntityUid toInsert, IEntityManager entMan);

        /// <summary>
        /// Implement to remove the reference you used to store the entity
        /// </summary>
        /// <param name="toRemove"></param>
        /// <param name="entMan"></param>
        protected abstract void InternalRemove(EntityUid toRemove, IEntityManager entMan);
    }
}
