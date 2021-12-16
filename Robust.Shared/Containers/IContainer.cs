using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// A container is a way to "contain" entities inside other entities, in a logical way.
    /// This is alike BYOND's <c>contents</c> system, except more advanced.
    /// </summary>
    /// <remarks>
    ///     <p>
    ///     Containers are logical separations of entities contained inside another entity.
    ///     for example, a crate with two separated compartments would have two separate containers.
    ///     If an entity inside compartment A drops something,
    ///     the dropped entity would be placed in compartment A too,
    ///     and compartment B would be completely untouched.
    ///     </p>
    ///     <p>
    ///     Containers are managed by an entity's <see cref="IContainerManager" />,
    ///     and have an ID to be referenced by.
    ///     </p>
    /// </remarks>
    /// <seealso cref="IContainerManager" />
    [PublicAPI]
    [ImplicitDataDefinitionForInheritors]
    public interface IContainer
    {
        /// <summary>
        /// Readonly collection of all the entities contained within this specific container
        /// </summary>
        IReadOnlyList<EntityUid> ContainedEntities { get; }

        List<EntityUid> ExpectedEntities { get; }

        /// <summary>
        /// The type of this container.
        /// </summary>
        string ContainerType { get; }

        /// <summary>
        /// True if the container has been shut down via <see cref="Shutdown" />
        /// </summary>
        bool Deleted { get; }

        /// <summary>
        /// The ID of this container.
        /// </summary>
        string ID { get; }

        /// <summary>
        /// The container manager owning this container.
        /// </summary>
        IContainerManager Manager { get; }

        /// <summary>
        /// Prevents light from escaping the container, from ex. a flashlight.
        /// </summary>
        bool OccludesLight { get; set; }

        /// <summary>
        /// The entity owning this container.
        /// </summary>
        EntityUid Owner { get; }

        /// <summary>
        /// Should the contents of this container be shown? False for closed containers like lockers, true for
        /// things like glass display cases.
        /// </summary>
        bool ShowContents { get; set; }

        /// <summary>
        /// Checks if the entity can be inserted into this container.
        /// </summary>
        /// <param name="toinsert">The entity to attempt to insert.</param>
        /// <param name="entMan"></param>
        /// <returns>True if the entity can be inserted, false otherwise.</returns>
        bool CanInsert(EntityUid toinsert, IEntityManager? entMan = null);

        /// <summary>
        /// Attempts to insert the entity into this container.
        /// </summary>
        /// <remarks>
        /// If the insertion is successful, the inserted entity will end up parented to the
        /// container entity, and the inserted entity's local position will be set to the zero vector.
        /// </remarks>
        /// <param name="toinsert">The entity to insert.</param>
        /// <param name="entMan"></param>
        /// <returns>False if the entity could not be inserted.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this container is a child of the entity,
        /// which would cause infinite loops.
        /// </exception>
        bool Insert(EntityUid toinsert, IEntityManager? entMan = null);

        /// <summary>
        /// Checks if the entity can be removed from this container.
        /// </summary>
        /// <param name="toremove">The entity to check.</param>
        /// <param name="entMan"></param>
        /// <returns>True if the entity can be removed, false otherwise.</returns>
        bool CanRemove(EntityUid toremove, IEntityManager? entMan = null);

        /// <summary>
        /// Attempts to remove the entity from this container.
        /// </summary>
        /// <param name="toremove">The entity to attempt to remove.</param>
        /// <param name="entMan"></param>
        /// <returns>True if the entity was removed, false otherwise.</returns>
        bool Remove(EntityUid toremove, IEntityManager? entMan = null);

        /// <summary>
        /// Forcefully removes an entity from the container. Normally you would want to use <see cref="Remove" />,
        /// this function should be avoided.
        /// </summary>
        /// <param name="toRemove">The entity to attempt to remove.</param>
        /// <param name="entMan"></param>
        void ForceRemove(EntityUid toRemove, IEntityManager? entMan = null);

        /// <summary>
        /// Checks if the entity is contained in this container.
        /// This is not recursive, so containers of children are not checked.
        /// </summary>
        /// <param name="contained">The entity to check.</param>
        /// <returns>True if the entity is immediately contained in this container, false otherwise.</returns>
        bool Contains(EntityUid contained);

        /// <summary>
        /// Clears the container and marks it as deleted.
        /// </summary>
        void Shutdown();
    }
}
