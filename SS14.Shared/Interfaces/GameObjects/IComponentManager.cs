using System;
using System.Collections.Generic;
using SS14.Shared.GameObjects;

namespace SS14.Shared.Interfaces.GameObjects
{
    /// <summary>
    ///     Holds a collection of ECS components that are attached to entities.
    /// </summary>
    public interface IComponentManager
    {
        /// <summary>
        ///     Instantly clears all components from the manager. This will NOT shut them down gracefully.
        ///     Any entities relying on existing components will be broken.
        /// </summary>
        void Clear();

        #region Component Management

        /// <summary>
        ///     Adds a Component type to an entity. If the entity is already Initialized, the component will
        ///     automatically be Initialized and Started.
        /// </summary>
        /// <typeparam name="T">Component type to add.</typeparam>
        /// <returns>The newly added component.</returns>
        T AddComponent<T>(IEntity entity)
            where T : Component, new();

        /// <summary>
        ///     Adds a Component to an entity. If the entity is already Initialized, the component will
        ///     automatically be Initialized and Started.
        /// </summary>
        /// <param name="entity">Entity being modified.</param>
        /// <param name="component">Component to add.</param>
        /// <param name="overwrite">Should it overwrite existing components?</param>
        void AddComponent(IEntity entity, Component component, bool overwrite = false);

        /// <summary>
        ///     Removes the component with the specified reference type,
        ///     Without needing to have the component itself.
        /// </summary>
        /// <typeparam name="T">The component reference type to remove.</typeparam>
        /// <param name="uid">Entity UID to modify.</param>
        void RemoveComponent<T>(EntityUid uid);

        /// <summary>
        ///     Removes the component with a specified type.
        /// </summary>
        /// <param name="uid">Entity UID to modify.</param>
        /// <param name="type">The component reference type to check.</param>
        void RemoveComponent(EntityUid uid, Type type);

        /// <summary>
        ///     Removes the component with a specified network ID.
        /// </summary>
        /// <param name="uid">Entity UID to modify.</param>
        /// <param name="netID">Network ID of the component to remove.</param>
        void RemoveComponent(EntityUid uid, uint netID);

        /// <summary>
        ///     Removes the specified component.
        /// </summary>
        /// <param name="uid">Entity UID to modify.</param>
        /// <param name="component">Component to remove.</param>
        void RemoveComponent(EntityUid uid, IComponent component);

        /// <summary>
        ///     Removes ALL components from an entity.
        /// </summary>
        /// <param name="uid">Entity UID to modify.</param>
        void RemoveComponents(EntityUid uid);

        /// <summary>
        ///     Checks if the entity has a component type.
        /// </summary>
        /// <typeparam name="T">Component reference type to check for.</typeparam>
        /// <param name="uid">Entity UID to check.</param>
        /// <returns>True if the entity has the component type, otherwise false.</returns>
        bool HasComponent<T>(EntityUid uid);

        /// <summary>
        ///     Checks if the entity has a component type.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="type">Component reference type to check for.</param>
        /// <returns>True if the entity has the component type, otherwise false.</returns>
        bool HasComponent(EntityUid uid, Type type);

        /// <summary>
        ///     Checks if the entity has a component with a given network ID.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="netID">Network ID to tech for.</param>
        /// <returns>True if the entity has a component with the given network ID, otherwise false.</returns>
        bool HasComponent(EntityUid uid, uint netID);

        /// <summary>
        ///     Returns the component of a specific type.
        /// </summary>
        /// <typeparam name="T">Type of component to retrieve.</typeparam>
        /// <param name="uid">Entity UID to look on.</param>
        /// <returns>The component of Type from the Entity.</returns>
        T GetComponent<T>(EntityUid uid)
            where T : Component;

        /// <summary>
        ///     Returns the component of a specific type.
        /// </summary>
        /// <param name="uid">Entity UID to look on.</param>
        /// <param name="type">Type of component to retrieve.</param>
        /// <returns>The component of Type from the Entity.</returns>
        IComponent GetComponent(EntityUid uid, Type type);

        /// <summary>
        ///     Returns the component with a specific network ID.
        /// </summary>
        /// <param name="uid">Entity UID to look on.</param>
        /// <param name="netID">Network ID of the component to retrieve.</param>
        /// <returns>The component with the specified network id.</returns>
        IComponent GetComponent(EntityUid uid, uint netID);

        /// <summary>
        ///     Returns the component of a specific type.
        /// </summary>
        /// <typeparam name="T">Component reference type to check for.</typeparam>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="component">Component of the specified type (if exists).</param>
        /// <returns>If the component existed in the entity.</returns>
        bool TryGetComponent<T>(EntityUid uid, out T component)
            where T : class;

        /// <summary>
        ///     Returns the component of a specific type.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="type">Component reference type to check for.</param>
        /// <param name="component">Component of the specified type (if exists).</param>
        /// <returns>If the component existed in the entity.</returns>
        bool TryGetComponent(EntityUid uid, Type type, out IComponent component);

        /// <summary>
        ///     Returns the component with a specified network ID.
        /// </summary>
        /// <param name="uid">Entity UID to check.</param>
        /// <param name="netID">Component Network ID to check for.</param>
        /// <param name="component">Component with the specified network id.</param>
        /// <returns>If the component existed in the entity.</returns>
        bool TryGetComponent(EntityUid uid, uint netID, out IComponent component);

        /// <summary>
        ///     Returns ALL component type instances on an entity. A single component instance
        ///     can have multiple component types.
        /// </summary>
        /// <param name="uid">Entity UID to look on.</param>
        /// <returns>All component types on the Entity.</returns>
        IEnumerable<IComponent> GetComponents(EntityUid uid);

        /// <summary>
        ///     Returns ALL component type instances that are assignable to the specified type.
        ///     A single component instance can have multiple component type instances.
        /// </summary>
        /// <typeparam name="T">Type to filter.</typeparam>
        /// <param name="uid">Entity UID to look on.</param>
        /// <returns>All components that are assignable to the specified type.</returns>
        IEnumerable<T> GetComponents<T>(EntityUid uid);

        /// <summary>
        ///     Returns ALL networked components on an entity.
        /// </summary>
        /// <param name="uid">Entity UID to look on.</param>
        /// <returns>All components that have a network ID.</returns>
        IEnumerable<IComponent> GetNetComponents(EntityUid uid);

        /// <summary>
        ///     Returns ALL component instances of a specified type.
        /// </summary>
        /// <typeparam name="T">Type to filter.</typeparam>
        /// <returns>All components that are a specified type.</returns>
        IEnumerable<T> GetAllComponents<T>()
            where T : IComponent;

        /// <summary>
        ///     Culls all components from the collection that are marked as deleted. This needs to be called often.
        /// </summary>
        void CullRemovedComponents();

        #endregion
    }
}
