using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
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
    [ImplicitDataDefinitionForInheritors]
    public abstract partial class BaseContainer
    {
        /// <summary>
        /// Readonly collection of all the entities contained within this specific container
        /// </summary>
        public abstract IReadOnlyList<EntityUid> ContainedEntities { get; }

        // VV convenience field
        [ViewVariables]
        private IReadOnlyList<NetEntity> NetContainedEntities => ContainedEntities
            .Select(o => IoCManager.Resolve<IEntityManager>().GetNetEntity(o)).ToList();

        /// <summary>
        /// Number of contained entities.
        /// </summary>
        public abstract int Count { get; }

        [ViewVariables, NonSerialized]
        public List<NetEntity> ExpectedEntities = new();

        /// <summary>
        /// The ID of this container.
        /// </summary>
        [ViewVariables, NonSerialized, Access(typeof(SharedContainerSystem), typeof(ContainerManagerComponent))]
        public string ID = default!;

        [NonSerialized]
        internal ContainerManagerComponent Manager = default!;

        /// <summary>
        /// Prevents light from escaping the container, from ex. a flashlight.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("occludes")]
        public bool OccludesLight { get; set; } = true;

        /// <summary>
        /// The entity that owns this container.
        /// </summary>
        [ViewVariables]
        public EntityUid Owner { get; internal set; }

        /// <summary>
        /// Should the contents of this container be shown? False for closed containers like lockers, true for
        /// things like glass display cases.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("showEnts")]
        public bool ShowContents { get; set; }

        internal void Init(string id, EntityUid owner, ContainerManagerComponent component)
        {
            DebugTools.AssertNull(ID);
            ID = id;
            Manager = component;
            Owner = owner;
        }

        [Obsolete("Use container system method")]
        public bool Insert(
            EntityUid toinsert,
            IEntityManager? entMan = null,
            TransformComponent? transform = null,
            TransformComponent? ownerTransform = null,
            MetaDataComponent? meta = null,
            PhysicsComponent? physics = null,
            bool force = false)
        {
            IoCManager.Resolve(ref entMan);
            DebugTools.AssertOwner(toinsert, transform);
            DebugTools.AssertOwner(Owner, ownerTransform);
            DebugTools.AssertOwner(toinsert, physics);
            DebugTools.Assert(!ExpectedEntities.Contains(entMan.GetNetEntity(toinsert)));
            DebugTools.Assert(Manager.Containers.ContainsKey(ID));

            var physicsQuery = entMan.GetEntityQuery<PhysicsComponent>();
            var transformQuery = entMan.GetEntityQuery<TransformComponent>();
            var jointQuery = entMan.GetEntityQuery<JointComponent>();

            // ECS containers when
            var physicsSys = entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>();
            var jointSys = entMan.EntitySysManager.GetEntitySystem<SharedJointSystem>();
            var containerSys = entMan.EntitySysManager.GetEntitySystem<SharedContainerSystem>();

            // If someone is attempting to insert an entity into a container that is getting deleted, then we will
            // automatically delete that entity. I.e., the insertion automatically "succeeds" and both entities get deleted.
            // This is consistent with what happens if you attempt to attach an entity to a terminating parent.

            if (!entMan.TryGetComponent(Owner, out MetaDataComponent? ownerMeta))
            {
                Logger.ErrorS("container",
                    $"Attempted to insert an entity {entMan.ToPrettyString(toinsert)} into a non-existent entity.");
                entMan.QueueDeleteEntity(toinsert);
                return false;
            }

            if (ownerMeta.EntityLifeStage >= EntityLifeStage.Terminating)
            {
                Logger.ErrorS("container",
                    $"Attempted to insert an entity {entMan.ToPrettyString(toinsert)} into an entity that is terminating. Entity: {entMan.ToPrettyString(Owner)}.");
                entMan.QueueDeleteEntity(toinsert);
                return false;
            }

            transform ??= transformQuery.GetComponent(toinsert);

            //Verify we can insert into this container
            if (!force && !containerSys.CanInsert(toinsert, this, containerXform: ownerTransform))
                return false;

            // Please somebody ecs containers
            var lookupSys = entMan.EntitySysManager.GetEntitySystem<EntityLookupSystem>();
            var xformSys = entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>();

            meta ??= entMan.GetComponent<MetaDataComponent>(toinsert);
            if (meta.EntityLifeStage >= EntityLifeStage.Terminating)
            {
                Logger.ErrorS("container",
                    $"Attempted to insert a terminating entity {entMan.ToPrettyString(toinsert)} into a container {ID} in entity: {entMan.ToPrettyString(Owner)}.");
                return false;
            }

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
            lookupSys.RemoveFromEntityTree(toinsert, transform);
            DebugTools.Assert(transform.Broadphase == null || !transform.Broadphase.Value.IsValid());

            // Avoid unnecessary broadphase updates while unanchoring, changing physics collision, and re-parenting.
            var old = transform.Broadphase;
            transform.Broadphase = BroadphaseData.Invalid;

            // Unanchor the entity (without changing physics body types).
            xformSys.Unanchor(toinsert, transform, false);

            // Next, update physics. Note that this cannot just be done in the physics system via parent change events,
            // because the insertion may not result in a parent change. This could alternatively be done via a
            // got-inserted event, but really that event should run after the entity was actually inserted (so that
            // parent/map have updated). But we are better of disabling collision before doing map/parent changes.
            physicsQuery.Resolve(toinsert, ref physics, false);
            RecursivelyUpdatePhysics(toinsert, transform, physics, physicsSys, physicsQuery, transformQuery);

            // Attach to new parent
            var oldParent = transform.ParentUid;
            xformSys.SetCoordinates(toinsert, transform, new EntityCoordinates(Owner, Vector2.Zero), Angle.Zero);
            transform.Broadphase = old;

            // the transform.AttachParent() could previously result in the flag being unset, so check that this hasn't happened.
            DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) != 0);

            // Implementation specific insert logic
            InternalInsert(toinsert, entMan);

            // Update any relevant joint relays
            // Can't be done above as the container flag isn't set yet.
            RecursivelyUpdateJoints(toinsert, transform, jointSys, jointQuery, transformQuery);

            // Raise container events (after re-parenting and internal remove).
            entMan.EventBus.RaiseLocalEvent(Owner, new EntInsertedIntoContainerMessage(toinsert, oldParent, this), true);
            entMan.EventBus.RaiseLocalEvent(toinsert, new EntGotInsertedIntoContainerMessage(toinsert, this), true);

            // The sheer number of asserts tells you about how little I trust container and parenting code.
            DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) != 0);
            DebugTools.Assert(!transform.Anchored);
            DebugTools.Assert(transform.LocalPosition == Vector2.Zero);
            DebugTools.Assert(transform.LocalRotation == Angle.Zero);
            DebugTools.Assert(!physicsQuery.TryGetComponent(toinsert, out var phys) || (!phys.Awake && !phys.CanCollide));

            entMan.Dirty(Owner, Manager);
            return true;
        }

        internal void RecursivelyUpdatePhysics(
            EntityUid uid,
            TransformComponent xform,
            PhysicsComponent? physics,
            SharedPhysicsSystem physicsSys,
            EntityQuery<PhysicsComponent> physicsQuery,
            EntityQuery<TransformComponent> transformQuery)
        {
            if (physics != null)
            {
                // Here we intentionally don't dirty the physics comp. Client-side state handling will apply these same
                // changes. This also ensures that the server doesn't have to send the physics comp state to every
                // player for any entity inside of a container during init.
                physicsSys.SetLinearVelocity(uid, Vector2.Zero, false, body: physics);
                physicsSys.SetAngularVelocity(uid,0, false, body: physics);
                physicsSys.SetCanCollide(uid, false, false, body: physics);
            }

            var enumerator = xform.ChildEnumerator;

            while (enumerator.MoveNext(out var child))
            {
                var childXform = transformQuery.GetComponent(child.Value);
                physicsQuery.TryGetComponent(child.Value, out var childPhysics);
                RecursivelyUpdatePhysics(child.Value, childXform, childPhysics, physicsSys, physicsQuery, transformQuery);
            }
        }

        internal void RecursivelyUpdateJoints(
            EntityUid uid,
            TransformComponent xform,
            SharedJointSystem jointSys,
            EntityQuery<JointComponent> jointQuery,
            EntityQuery<TransformComponent> transformQuery)
        {
            if (jointQuery.TryGetComponent(uid, out var jointComp))
            {
                // TODO: This is going to be going up while joints going down, although these aren't too common
                // in SS14 atm.
                jointSys.RefreshRelay(uid, jointComp);
            }

            var enumerator = xform.ChildEnumerator;

            while (enumerator.MoveNext(out var child))
            {
                var childXform = transformQuery.GetComponent(child.Value);
                RecursivelyUpdateJoints(child.Value, childXform, jointSys, jointQuery, transformQuery);
            }
        }

        /// <summary>
        /// Whether the given entity can be inserted into this container.
        /// </summary>
        /// <param name="assumeEmpty">Whether to assume that the container is currently empty.</param>
        protected internal virtual bool CanInsert(EntityUid toInsert, bool assumeEmpty, IEntityManager entMan) => true;

        [Obsolete("Use container system method")]
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
            DebugTools.AssertNotNull(Manager);
            DebugTools.Assert(entMan.EntityExists(toRemove));
            DebugTools.AssertOwner(toRemove, xform);
            DebugTools.AssertOwner(toRemove, meta);

            xform ??= entMan.GetComponent<TransformComponent>(toRemove);
            meta ??= entMan.GetComponent<MetaDataComponent>(toRemove);

            var sys = entMan.EntitySysManager.GetEntitySystem<SharedContainerSystem>();
            if (!force && !sys.CanRemove(toRemove, this))
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
                entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().SetCoordinates(toRemove, xform, destination.Value, localRotation);
            }
            else if (reparent)
            {
                // Container ECS when.
                sys.AttachParentToContainerOrGrid((toRemove, xform));
                if (localRotation != null)
                    entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().SetLocalRotation(xform, localRotation.Value);
            }

            // Add to new broadphase
            if (xform.ParentUid == oldParent // move event should already have handled it
                && xform.Broadphase == null) // broadphase explicitly invalid?
            {
                entMan.EntitySysManager.GetEntitySystem<EntityLookupSystem>().FindAndAddToEntityTree(toRemove, xform: xform);
            }

            if (entMan.TryGetComponent<JointComponent>(toRemove, out var jointComp))
            {
                entMan.System<SharedJointSystem>().RefreshRelay(toRemove, jointComp);
            }

            // Raise container events (after re-parenting and internal remove).
            entMan.EventBus.RaiseLocalEvent(Owner, new EntRemovedFromContainerMessage(toRemove, this), true);
            entMan.EventBus.RaiseLocalEvent(toRemove, new EntGotRemovedFromContainerMessage(toRemove, this), false);

            DebugTools.Assert(destination == null || xform.Coordinates.Equals(destination.Value));

            entMan.Dirty(Owner, Manager);
            return true;
        }

        [Obsolete("Use container system method")]
        public void ForceRemove(EntityUid toRemove, IEntityManager? entMan = null, MetaDataComponent? meta = null)
            => Remove(toRemove, entMan, meta: meta, reparent: false, force: true);

        /// <summary>
        /// Checks if the entity is contained in this container.
        /// This is not recursive, so containers of children are not checked.
        /// </summary>
        /// <param name="contained">The entity to check.</param>
        /// <returns>True if the entity is immediately contained in this container, false otherwise.</returns>
        public abstract bool Contains(EntityUid contained);

        /// <summary>
        /// Clears the container and marks it as deleted.
        /// </summary>
        public void Shutdown(IEntityManager? entMan = null, INetManager? netMan = null)
        {
            IoCManager.Resolve(ref entMan, ref netMan);
            InternalShutdown(entMan, netMan.IsClient);
            Manager.Containers.Remove(ID);
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
