using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SS14.Shared.IoC;
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
        IEnumerable<T> GetComponents<T>() where T: IComponent;

        /// <summary>
        /// Add a component to the master component list.
        /// </summary>
        /// <param name="component">The component to add.</param>
        void AddComponent(IComponent component);

        /// <summary>
        /// Remove a component from the master component list.
        /// </summary>
        /// <param name="component"></param>
        void RemoveComponent(IComponent component);

        /// <summary>
        /// Clear the master component list
        /// </summary>
        void Cull();

        /// <summary>
        /// Big update method -- loops through all components in order of family and calls Update() on them.
        /// </summary>
        /// <param name="frameTime">Time since the last frame was rendered.</param>
        void Update(float frameTime);
    }
}
