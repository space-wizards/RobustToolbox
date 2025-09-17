using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Interface used to allow the map loader to override prototype data with map data.
    /// </summary>
    internal interface IEntityLoadContext
    {
        /// <summary>Tries getting the data of the given component.</summary>
        /// <param name="componentName">Name of component to find.</param>
        /// <param name="component">Found component or null.</param>
        /// <returns>True if the component was found, false otherwise.</returns>
        /// <seealso cref="TryGetComponent{T}"/>
        bool TryGetComponent(string componentName, [NotNullWhen(true)] out IComponent? component);

        /// <summary>Tries getting the data of the given component.</summary>
        /// <typeparam name="TComponent">Type of component to be found.</typeparam>
        /// <param name="componentFactory">Component factory required for the lookup.</param>
        /// <param name="component">Found component or null.</param>
        /// <returns>True if the component was found, false otherwise.</returns>
        /// <seealso cref="TryGetComponent"/>
        bool TryGetComponent<TComponent>(
            IComponentFactory componentFactory,
            [NotNullWhen(true)] out TComponent? component
        ) where TComponent : class, IComponent, new();

        /// <summary>
        /// Gets all components registered for the entityloadcontext, overrides as well as extra components
        /// </summary>
        IEnumerable<string> GetExtraComponentTypes();

        /// <summary>
        /// Checks whether a given component should be added to an entity.
        /// Used to prevent certain prototype components from being added while spawning an entity.
        /// </summary>
        bool ShouldSkipComponent(string compName);
    }
}
