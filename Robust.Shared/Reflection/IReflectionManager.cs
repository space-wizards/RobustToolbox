using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Robust.Shared.Reflection
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
    public interface IReflectionManager
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
        IEnumerable<Type> GetAllChildren<T>(bool inclusive = false);

        /// <summary>
        /// Gets all known types that are assignable to the given type.
        /// </summary>
        /// <param name="baseType">The base type to search for.</param>
        /// <param name="inclusive">When <code>true</code>, include <typeparamref name="T"/> in the
        /// returned results if it is a known type.</param>
        /// <returns>An enumerable over all the types. Order is in no way guaranteed.</returns>
        IEnumerable<Type> GetAllChildren(Type baseType, bool inclusive = false);

        /// <summary>
        /// All loaded assemblies.
        /// </summary>
        IReadOnlyList<Assembly> Assemblies { get; }

        /// <summary>
        /// Attempts to get a type by string name in the loaded assemblies.
        /// </summary>
        /// <param name="name">
        /// The type name to look up. Anything accepted by <see cref="Type.GetType"/> works.
        /// However, if the type does not start with <code>Robust.*</code> and cannot be found,
        /// it will add <code>Robust.Client</code>, <code>Robust.Shared</code>, etc... in front of it.
        /// </param>
        /// <returns></returns>
        Type? GetType(string name);

        Type LooseGetType(string name);

        bool TryLooseGetType(string name, [NotNullWhen(true)] out Type? type);

        /// <summary>
        /// Finds all Types in all Assemblies that have a specific Attribute.
        /// </summary>
        /// <typeparam name="T">Attribute to search for.</typeparam>
        /// <returns>Enumeration of all types with the specified attribute.</returns>
        IEnumerable<Type> FindTypesWithAttribute<T>() where T : Attribute;

        /// <summary>
        /// Finds all Types in all Assemblies that have a specific Attribute.
        /// </summary>
        /// <param name="attributeType">Attribute to search for.</param>
        /// <returns>Enumeration of all types with the specified attribute.</returns>
        IEnumerable<Type> FindTypesWithAttribute(Type attributeType);

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
        event EventHandler<ReflectionUpdateEventArgs>? OnAssemblyAdded;

        /// <summary>
        ///     Tries to parse an enum in the form "enum.PowerStorageAppearance.Charge", for use in prototyping.
        /// </summary>
        /// <param name="reference">
        ///     The string enum reference, including the "enum." prefix.
        ///     If this prefix does not exist, it is assumed to not be a reference and ignored.</param>
        /// <param name="enum"></param>
        /// <returns>
        ///     True if the string was an enum reference that parsed correctly, false if it was not a reference.
        ///     Note that if it was a reference and it could not be resolved, the function throws a <see cref="ArgumentException"/> instead.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///     Thrown if this string is an enum reference, but the enum could not be resolved.
        /// </exception>
        bool TryParseEnumReference(string reference, [NotNullWhen(true)] out Enum? @enum);

        Type? YamlTypeTagLookup(Type baseType, string typeName);
    }
}