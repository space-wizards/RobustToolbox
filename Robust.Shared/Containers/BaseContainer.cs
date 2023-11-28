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
            return entMan.System<SharedContainerSystem>().Insert((toinsert, transform, meta, physics), this, ownerTransform, force);
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
            Angle? localRotation = null
        )
        {
            IoCManager.Resolve(ref entMan);
            return entMan.System<SharedContainerSystem>().Remove((toRemove, xform, meta), this, reparent, force, destination, localRotation);
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
        [Access(typeof(SharedContainerSystem))]
        protected internal abstract void InternalInsert(EntityUid toInsert, IEntityManager entMan);

        /// <summary>
        /// Implement to remove the reference you used to store the entity
        /// </summary>
        /// <param name="toRemove"></param>
        /// <param name="entMan"></param>
        [Access(typeof(SharedContainerSystem))]
        protected internal abstract void InternalRemove(EntityUid toRemove, IEntityManager entMan);
    }
}
