using System;
using System.Collections.Generic;
using SS14.Shared.GameObjects;

namespace SS14.Shared.Interfaces.GameObjects
{
    public interface IComponentManager
    {
        /// <summary>
        /// Gets an <see cref="IEnumerable{IComponent}"/> over every known <see cref="IComponent"/> with a specified <see cref="ComponentFamily"/>.
        /// </summary>
        /// <param name="family">The <see cref="ComponentFamily"/> to look up.</param>
        /// <returns>An <see cref="IEnumerable{IComponent}"/> over component with the specified family.</returns>
        IEnumerable<T> GetComponents<T>()
            where T : IComponent;

        /// <summary>
        /// Add a component to the master component list.
        /// </summary>
        /// <param name="component">The component to add.</param>
        void AddComponentOld(IComponent component);

        /// <summary>
        /// Clear the master component list
        /// </summary>
        void Cull();

        /// <summary>
        /// Big update method -- loops through all components in order of family and calls Update() on them.
        /// </summary>
        /// <param name="frameTime">Time since the last frame was rendered.</param>
        void Update(float frameTime);

        #region Component Management

        T AddComponent<T>(IEntity entity)
            where T : Component, new();

        void AddComponent(IEntity entity, Component component, bool overwrite = false);

        void RemoveComponent<T>(EntityUid uid);
        void RemoveComponent(EntityUid uid, Type type);
        void RemoveComponent(EntityUid uid, uint netID);
        void RemoveComponent(EntityUid uid, IComponent component);
        void RemoveComponents(EntityUid uid);

        bool HasComponent<T>(EntityUid uid);
        bool HasComponent(EntityUid uid, Type type);
        bool HasComponent(EntityUid uid, uint netID);

        T GetComponent<T>(EntityUid uid)
            where T : Component;

        IComponent GetComponent(EntityUid uid, Type type);
        IComponent GetComponent(EntityUid uid, uint netID);

        bool TryGetComponent<T>(EntityUid uid, out T component)
            where T : class;

        bool TryGetComponent(EntityUid uid, Type type, out IComponent component);
        bool TryGetComponent(EntityUid uid, uint netID, out IComponent component);

        IEnumerable<IComponent> GetComponents(EntityUid uid);
        IEnumerable<T> GetComponents<T>(EntityUid uid);
        IEnumerable<IComponent> GetNetComponents(EntityUid uid);

        void CullDeletedComponents();

        #endregion
    }
}
