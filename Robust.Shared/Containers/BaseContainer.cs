using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

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
        public IContainerManager Manager { get; internal set; } = default!; // Make sure you set me in init

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

            transform ??= transformQuery.GetComponent(toinsert);
            meta ??= entMan.GetComponent<MetaDataComponent>(toinsert);

            // remove from any old containers.
            if ((meta.Flags & MetaDataFlags.InContainer) != 0 &&
                entMan.TryGetComponent(transform.ParentUid, out ContainerManagerComponent? oldManager) &&
                oldManager.TryGetContainer(toinsert, out var oldContainer) &&
                !oldContainer.Remove(toinsert, entMan, transform, meta, false))
            {
                // failed to remove from container --> cannot insert.
                return false;
            }

            // Update metadata first, so that parent change events can check IsInContainer.
            meta.Flags |= MetaDataFlags.InContainer;

            // Next, update physics. Note that this cannot just be done in the physics system via parent change events,
            // because the insertion may not result in a parent change. This could alternatively be done via a
            // got-inserted event, but really that event should run after the entity was actually inserted (so that
            // parent/map have updated). But we are better of disabling collision before doing map/parent changes.
            if (physics == null)
                physicsQuery.TryGetComponent(toinsert, out physics);
            RecursivelyUpdatePhysics(transform, physics, physicsSys, jointSys, physicsQuery, transformQuery, jointQuery);

            ownerTransform ??= transformQuery.GetComponent(Owner);
            var oldParent = transform.ParentUid;
            transform.AttachParent(ownerTransform);
            InternalInsert(toinsert, oldParent, entMan);

            // the transform.AttachParent() could previously result in the flag being unset, so check that this hasn't happened.
            DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) != 0);

            // This is an edge case where the parent grid is the container being inserted into, so AttachParent would not unanchor.
            if (transform.Anchored)
                transform.Anchored = false;

            // spatially move the object to the location of the container. If you don't want this functionality, the
            // calling code can save the local position before calling this function, and apply it afterwords.
            transform.LocalPosition = Vector2.Zero;
            transform.LocalRotation = Angle.Zero;

            DebugTools.Assert(!physicsQuery.TryGetComponent(toinsert, out var phys) || !phys.Awake);
            return true;
        }

        private void RecursivelyUpdatePhysics(TransformComponent xform,
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
                    jointSys.ClearJoints(joint);
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
            if (entMan.HasComponent<IMapComponent>(toinsert) || entMan.HasComponent<IMapGridComponent>(toinsert))
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
        public bool Remove(EntityUid toremove, IEntityManager? entMan = null, TransformComponent? xform = null, MetaDataComponent? meta = null, bool reparent = true)
        {
            DebugTools.Assert(!Deleted);
            DebugTools.AssertNotNull(Manager);
            DebugTools.AssertNotNull(toremove);
            IoCManager.Resolve(ref entMan);
            DebugTools.Assert(entMan.EntityExists(toremove));
            DebugTools.Assert(xform == null || xform.Owner == toremove);

            if (!CanRemove(toremove, entMan)) return false;
            InternalRemove(toremove, entMan, meta);

            if (reparent)
            {
                xform ??= entMan.GetComponent<TransformComponent>(toremove);
                xform.AttachParentToContainerOrGrid(entMan);
            }

            return true;
        }

        /// <inheritdoc />
        public void ForceRemove(EntityUid toRemove, IEntityManager? entMan = null, MetaDataComponent? meta = null)
        {
            DebugTools.Assert(!Deleted);
            DebugTools.AssertNotNull(Manager);
            DebugTools.AssertNotNull(toRemove);
            IoCManager.Resolve(ref entMan);
            DebugTools.Assert(entMan.EntityExists(toRemove));

            InternalRemove(toRemove, entMan, meta);
        }

        /// <inheritdoc />
        public virtual bool CanRemove(EntityUid toremove, IEntityManager? entMan = null)
        {
            DebugTools.Assert(!Deleted);

            if (!Contains(toremove))
                return false;

            IoCManager.Resolve(ref entMan);

            //raise events
            var removeAttemptEvent = new ContainerIsRemovingAttemptEvent(this, toremove);
            entMan.EventBus.RaiseLocalEvent(Owner, removeAttemptEvent, true);
            if (removeAttemptEvent.Cancelled)
                return false;

            var gettingRemovedAttemptEvent = new ContainerGettingRemovedAttemptEvent(this, toremove);
            entMan.EventBus.RaiseLocalEvent(toremove, gettingRemovedAttemptEvent, true);
            if (gettingRemovedAttemptEvent.Cancelled)
                return false;

            return true;
        }

        /// <inheritdoc />
        public abstract bool Contains(EntityUid contained);

        /// <inheritdoc />
        public virtual void Shutdown()
        {
            Manager.InternalContainerShutdown(this);
            Deleted = true;
        }

        /// <summary>
        /// Implement to store the reference in whatever form you want
        /// </summary>
        /// <param name="toinsert"></param>
        /// <param name="entMan"></param>
        protected virtual void InternalInsert(EntityUid toinsert, EntityUid oldParent, IEntityManager entMan)
        {
            DebugTools.Assert(!Deleted);
            entMan.EventBus.RaiseLocalEvent(Owner, new EntInsertedIntoContainerMessage(toinsert, oldParent, this), true);
            Manager.Dirty(entMan);
        }

        /// <summary>
        /// Implement to remove the reference you used to store the entity
        /// </summary>
        /// <param name="toremove"></param>
        /// <param name="entMan"></param>
        protected virtual void InternalRemove(EntityUid toremove, IEntityManager entMan, MetaDataComponent? meta = null)
        {
            DebugTools.Assert(!Deleted);
            DebugTools.AssertNotNull(Manager);
            DebugTools.AssertNotNull(toremove);
            DebugTools.Assert(entMan.EntityExists(toremove));
            DebugTools.Assert(meta == null || meta.Owner == toremove);

            meta ??= entMan.GetComponent<MetaDataComponent>(toremove);
            meta.Flags &= ~MetaDataFlags.InContainer;
            entMan.EventBus.RaiseLocalEvent(Owner, new EntRemovedFromContainerMessage(toremove, this), true);
            entMan.EventBus.RaiseLocalEvent(toremove, new EntGotRemovedFromContainerMessage(toremove, this), false);
            Manager.Dirty(entMan);
        }
    }
}
