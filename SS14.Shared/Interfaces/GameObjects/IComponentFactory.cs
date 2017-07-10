using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.IoC;

namespace SS14.Shared.Interfaces.GameObjects
{
    /// <summary>
    /// Used by <see cref="Shared.GameObjects.EntityPrototype" /> to determine whether a component is available.
    /// This distinction is important because prototypes are shared across client and server, but the two might have different components.
    /// </summary>
    public enum ComponentAvailability
    {
        /// <summary>
        /// The component is available and can be insantiated.
        /// </summary>
        Available,

        /// <summary>
        /// The component is not available, but should be ignored (prevent warnings for missing components).
        /// </summary>
        Ignore,

        /// <summary>
        /// The component is unknown entirely. This may warrant a warning or error.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Handles the spawning of components.
    /// Does IoC magic to allow accessing components by <see cref="IComponent.Name"/>.
    /// </summary>
    public interface IComponentFactory
    {
        /// <summary>
        /// Get whether a component is available right now.
        /// </summary>
        /// <param name="componentName">The name of the component to check.</param>
        /// <returns>The availability of the component.</returns>
        ComponentAvailability GetComponentAvailability(string componentName);

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <param name="componentType">type of component to make</param>
        /// <returns>A Component</returns>
        IComponent GetComponent(Type componentType);

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <returns>A Component</returns>
        T GetComponent<T>() where T : IComponent;

        /// <summary>
        /// Gets a new component instantiated of the specified <see cref="IComponent.Name"/>.
        /// </summary>
        /// <param name="componentName">name of component to make</param>
        /// <returns>A Component</returns>
        IComponent GetComponent(string componentName);

        /// <summary>
        /// Get the <see cref="Type"/> which has the corresponding <see cref="IComponent.Name"/>.
        /// </summary>
        /// <param name="componentName">The <see cref="IComponent.Name"/> for the requested component.</param>
        /// <returns>A <see cref="Type"/> that has <see cref="IComponent.Name"/> equal to the provided name.</returns>
        Type GetComponentType(string componentName);
    }
}
