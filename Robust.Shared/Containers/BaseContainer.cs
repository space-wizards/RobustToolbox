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
        protected internal ContainerManagerComponent Manager = default!;

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
        [Obsolete("use system method")]
        public void Shutdown(IEntityManager? entMan = null, INetManager? _ = null)
        {
            IoCManager.Resolve(ref entMan);
            entMan.System<SharedContainerSystem>().ShutdownContainer(this);
        }

        /// <inheritdoc />
        [Access(typeof(SharedContainerSystem))]
        protected internal abstract void InternalShutdown(IEntityManager entMan, SharedContainerSystem system, bool isClient);

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
