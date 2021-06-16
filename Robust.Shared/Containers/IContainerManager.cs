using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers
{
    /// <summary>
    /// Manages containers on an entity.
    /// </summary>
    /// <seealso cref="IContainer" />
    public interface IContainerManager : IComponent
    {
        /// <summary>
        /// Makes a new container of the specified type.
        /// </summary>
        /// <param name="id">The ID for the new container.</param>
        /// <typeparam name="T">The type of the new container</typeparam>
        /// <returns>The new container.</returns>
        /// <exception cref="ArgumentException">Thrown if there already is a container with the specified ID</exception>
        T MakeContainer<T>(string id)
            where T : IContainer;

        /// <summary>
        /// Attempts to remove <paramref name="entity" /> contained inside the owning entity,
        /// finding the container containing it automatically, if it is actually contained.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        /// <returns>True if the entity was successfuly removed.</returns>
        bool Remove(IEntity entity);

        /// <summary>
        /// Gets the container with the specified ID.
        /// </summary>
        /// <param name="id">The ID to look up.</param>
        /// <returns>The container.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the container does not exist.</exception>
        IContainer GetContainer(string id);

        /// <summary>
        /// Checks whether we have a container with the specified ID.
        /// </summary>
        /// <param name="id">The entity ID to check.</param>
        /// <returns>True if we already have a container, false otherwise.</returns>
        bool HasContainer(string id);

        /// <summary>
        /// Attempt to retrieve a container with specified ID.
        /// </summary>
        /// <param name="id">The ID to look up.</param>
        /// <param name="container">The container if it was found, <c>null</c> if not found.</param>
        /// <returns>True if the container was found, false otherwise.</returns>
        bool TryGetContainer(string id, [NotNullWhen(true)] out IContainer? container);

        /// <summary>
        /// Attempt to retrieve a container that contains a specific entity.
        /// </summary>
        /// <param name="entity">The entity that is inside the container.</param>
        /// <param name="container">The container if it was found, <c>null</c> if not found.</param>
        /// <returns>True if the container was found, false otherwise.</returns>
        /// <returns>True if the container was found, false otherwise.</returns>
        bool TryGetContainer(IEntity entity, [NotNullWhen(true)] out IContainer? container);

        bool ContainsEntity(IEntity entity);

        void ForceRemove(IEntity entity);

        /// <summary>
        /// DO NOT CALL THIS DIRECTLY. Call <see cref="IContainer.Shutdown" /> instead.
        /// </summary>
        void InternalContainerShutdown(IContainer container);
    }
}
