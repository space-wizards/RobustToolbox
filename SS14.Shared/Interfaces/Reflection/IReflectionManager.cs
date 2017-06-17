using SS14.Shared.IoC;
using SS14.Shared.Reflection;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace SS14.Shared.Interfaces.Reflection
{
    /// <summary>
    /// Manages common reflection operations, such as iterating over all subtypes of something.
    /// This is distinctly different from IoC: IoC manages services and DI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, all classes are "discoverable" by <see cref="IReflectionManager"/>.
    /// This can be overriden by assigning a <see cref="ReflectAttribute"/> and disabling discoverability.
    /// Classes which cannot be instantiated are also ignored (interfaces, abstracts)
    /// </para>
    /// <para>
    /// Types only become accessible when loaded using <see cref="LoadAssembly"/>.
    /// This is to prevent non-game assemblies from cluttering everything.
    /// </para>
    /// </remarks>
    /// <seealso cref="IoCManager"/>
    /// <seealso cref="ReflectAttribute"/>
    public interface IReflectionManager : IIoCInterface
    {
        /// <summary>
        /// Gets all known types that are assignable to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inclusive">
        /// When <code>true</code>, include <typeparamref name="T"/> in the returned results
        /// if it is a known type.
        /// </param>
        /// <returns>An enumerable over all the types. Order is in no way guaranteed.</returns>
        IEnumerable<Type> GetAllChildren<T>(bool inclusive=false);

        /// <summary>
        /// All loaded assemblies.
        /// </summary>
        IReadOnlyList<Assembly> Assemblies { get; }

        /// <summary>
        /// Attempts to get a type by string name in the loaded assemblies.
        /// </summary>
        /// <param name="name">
        /// The type name to look up. Anything accepted by <see cref="Type.GetType"/> works.
        /// However, if the type does not start with <code>SS14.*</code> and cannot be found,
        /// it will add <code>SS14.Client</code>, <code>SS14.Shared</code>, etc... in front of it.
        /// </param>
        /// <returns></returns>
        Type GetType(string name);

        /// <summary>
        /// Loads assemblies into the manager and get all the types.
        /// </summary>
        void LoadAssemblies(IEnumerable<Assembly> assemblies);

        /// <summary>
        /// Loads assemblies into the manager and get all the types.
        /// </summary>
        void LoadAssemblies(params Assembly[] args);

        /// <summary>
        /// Fired whenever an assembly is added through <see cref="LoadAssemblies"/>,
        /// this means more types might be available from <see cref="GetType(string)"/> and <see cref="GetAllChildren{T}(bool)"/>
        /// </summary>
        event EventHandler<ReflectionUpdateEventArgs> OnAssemblyAdded;
    }

    public class ReflectionUpdateEventArgs : EventArgs
    {
        public readonly IReflectionManager ReflectionManager;
        public ReflectionUpdateEventArgs(IReflectionManager reflectionManager)
        {
            ReflectionManager = reflectionManager;
        }
    }
}
