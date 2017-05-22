using SS14.Shared.IoC;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System;

namespace SS14.Shared.ContentLoader
{
    /// <summary>
    /// Handles the loading and management of the content assemblies (SS14.*.Content)
    /// </summary>
    public interface IContentLoader : IIoCInterface
    {
        /// <summary>
        /// Enumerable over all types in the loaded assemblies.
        /// </summary>
        IEnumerable<Type> Types { get; }
        /// <summary>
        /// All loaded assemblies.
        /// </summary>
        IReadOnlyList<Assembly> Assemblies { get; }

        /// <summary>
        /// Load an assembly into the content loader and get all the types.
        /// </summary>
        /// <remarks>
        /// This does not hook into IoC, so adding the assembly to IoC manually is still required.
        /// This should be done after calling this, so IoC hooks don't fire prematurely and things attempt to pull nonexistant types from the content loader.
        /// </remarks>
        void LoadAssembly(Assembly assembly);
        /// <summary>
        /// Get a specified type by name. Iterates over all loaded assemblies until it finds one that has the type, or returns null if none could be found.
        /// </summary>
        Type GetType(string type);

    }

    [IoCTarget]
    public class ContentLoader : IContentLoader
    {
        public IEnumerable<Type> Types => Assemblies.SelectMany((Assembly a) => a.GetTypes());

        private List<Assembly> assemblies = new List<Assembly>();
        public IReadOnlyList<Assembly> Assemblies => assemblies;

        public void LoadAssembly(Assembly assembly)
        {
            assemblies.Add(assembly);
        }

        public Type GetType(string type)
        {
            foreach (var assembly in Assemblies)
            {
                var theType = assembly.GetType(type);
                if (theType != null)
                {
                    return theType;
                }
            }
            return null;
        }
    }
}
