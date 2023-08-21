using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

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
        /// Attempts to remove the entity from some container on this entity.
        /// </summary>
        /// <param name="reparent">If false, this operation will not rigger a move or parent change event. Ignored if
        /// destination is not null</param>
        /// <param name="force">If true, this will not perform can-remove checks.</param>
        /// <param name="destination">Where to place the entity after removing. Avoids unnecessary broadphase updates.
        /// If not specified, and reparent option is true, then the entity will either be inserted into a parent
        /// container, the grid, or the map.</param>
        /// <param name="localRotation">Optional final local rotation after removal. Avoids redundant move events.</param>
        bool Remove(EntityUid toremove,
            TransformComponent? xform = null,
            MetaDataComponent? meta = null,
            bool reparent = true,
            bool force = false,
            EntityCoordinates? destination = null,
            Angle? localRotation = null);

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
        bool TryGetContainer(EntityUid entity, [NotNullWhen(true)] out IContainer? container);

        bool ContainsEntity(EntityUid entity);
    }
}
