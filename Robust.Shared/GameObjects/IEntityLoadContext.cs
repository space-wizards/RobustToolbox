using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    ///     Interface used to allow the map loader to override prototype data with map data.
    /// </summary>
    internal interface IEntityLoadContext
    {
        /// <summary>
        ///     Tries getting the data of the provided component
        /// </summary>
        bool TryGetComponent(string componentName, [NotNullWhen(true)] out IComponent? component);

        /// <summary>
        ///     Gets all components registered for the entityloadcontext, overrides as well as extra components
        /// </summary>
        IEnumerable<string> GetExtraComponentTypes();

        /// <summary>
        ///     Checks whether a given component should be added to an entity. Used to prevent certain prototype components from being added while spawning an entity.
        /// </summary>
        bool ShouldSkipComponent(string compName);
    }
}
