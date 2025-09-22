using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
        // Will be null until after the component has been initialized.
        protected SharedContainerSystem? System;

        [Access(typeof(SharedContainerSystem), typeof(ContainerManagerComponent))]
        internal void Init(SharedContainerSystem system, string id, Entity<ContainerManagerComponent> owner)
        {
            DebugTools.Assert(ID == null || ID == id);
            ID = id;
            Owner = owner;
            Manager = owner;
            System = system;
        }

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

        /// <summary>
        /// Checks if the entity is contained in this container.
        /// This is not recursive, so containers of children are not checked.
        /// </summary>
        /// <param name="contained">The entity to check.</param>
        /// <returns>True if the entity is immediately contained in this container, false otherwise.</returns>
        public abstract bool Contains(EntityUid contained);

        /// <summary>
        /// Whether the given entity can be inserted into this container.
        /// </summary>
        /// <param name="assumeEmpty">Whether to assume that the container is currently empty.</param>
        protected internal virtual bool CanInsert(EntityUid toInsert, bool assumeEmpty, IEntityManager entMan) => true;

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

        /// <summary>
        /// Implement to clear the container and mark it as deleted.
        /// </summary>
        /// <param name="entMan"></param>
        /// <param name="system"></param>
        /// <param name=isClient"></param>
        [Access(typeof(SharedContainerSystem))]
        protected internal abstract void InternalShutdown(IEntityManager entMan, SharedContainerSystem system, bool isClient);
    }
}
