using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.IoC;

namespace SS14.Shared.Interfaces.GameObjects
{
    /// <summary>
    /// Handles the spawning of components.
    /// Does IoC magic to allow accessing components by <see cref="IComponent.Name"/>.
    /// </summary>
    public interface IComponentFactory : IIoCInterface
    {
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
