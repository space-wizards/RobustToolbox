using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Reflection;

namespace Robust.Shared.IoC
{
    /// <summary>
    /// The IoCManager handles Dependency Injection in the project.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dependency Injection is a concept where instead of saying "I need the <c>EntityManager</c>",
    /// you say "I need something that implements <c>IEntityManager</c>".
    /// This decouples the various systems into swappable components that have standardized interfaces.
    /// </para>
    /// <para>
    /// This is useful for a couple of things.
    /// Firstly, it allows the shared code to request the client or server code implicitly, without hacks.
    /// Secondly, it's very useful for unit tests as we can replace components to test things.
    /// </para>
    /// <para>
    /// To use the IoCManager, it first needs some types registered through <see cref="Register{TInterface, TImplementation}"/>.
    /// These implementations can then be fetched with <see cref="Resolve{T}"/>, or through field injection with <see cref="DependencyAttribute" />.
    /// </para>
    /// </remarks>
    /// <seealso cref="IReflectionManager"/>
    public interface IDependencyCollection
    {
        /// <summary>
        /// Registers an interface to an implementation, to make it accessible to <see cref="DependencyCollection.Resolve{T}"/>
        /// <see cref="IDependencyCollection.BuildGraph"/> MUST be called after this method to make the new interface available.
        /// </summary>
        /// <typeparam name="TInterface">The type that will be resolvable.</typeparam>
        /// <typeparam name="TImplementation">The type that will be constructed as implementation.</typeparam>
        /// <param name="overwrite">
        /// If true, do not throw an <see cref="InvalidOperationException"/> if an interface is already registered,
        /// replace the current implementation instead.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <paramref name="overwrite"/> is false and <typeparamref name="TInterface"/> has been registered before,
        /// or if an already instantiated interface (by <see cref="DependencyCollection.BuildGraph"/>) is attempting to be overwritten.
        /// </exception>
        void Register<TInterface, TImplementation>(bool overwrite = false)
            where TImplementation : class, TInterface;

        /// <summary>
        /// Registers an interface to an implementation, to make it accessible to <see cref="DependencyCollection.Resolve{T}"/>
        /// <see cref="IDependencyCollection.BuildGraph"/> MUST be called after this method to make the new interface available.
        /// </summary>
        /// <typeparam name="TInterface">The type that will be resolvable.</typeparam>
        /// <typeparam name="TImplementation">The type that will be constructed as implementation.</typeparam>
        /// <param name="factory">A factory method to construct the instance of the implementation.</param>
        /// <param name="overwrite">
        /// If true, do not throw an <see cref="InvalidOperationException"/> if an interface is already registered,
        /// replace the current implementation instead.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <paramref name="overwrite"/> is false and <typeparamref name="TInterface"/> has been registered before,
        /// or if an already instantiated interface (by <see cref="DependencyCollection.BuildGraph"/>) is attempting to be overwritten.
        /// </exception>
        void Register<TInterface, TImplementation>(DependencyFactoryDelegate<TImplementation> factory, bool overwrite = false)
            where TImplementation : class, TInterface;


        /// <summary>
        /// Registers a simple implementation without an interface.
        /// </summary>
        /// <param name="implementation">The type that will be resolvable.</param>
        /// <param name="factory">A factory method to construct the instance of the implementation.</param>
        /// <param name="overwrite">
        /// If true, do not throw an <see cref="InvalidOperationException"/> if an interface is already registered,
        /// replace the current implementation instead.
        /// </param>
        void Register(Type implementation, DependencyFactoryDelegate<object>? factory = null, bool overwrite = false);

        /// <summary>
        ///     Registers an interface to an existing instance of an implementation,
        ///     making it accessible to <see cref="IDependencyCollection.Resolve{T}"/>.
        ///     Unlike <see cref="IDependencyCollection.Register{TInterface, TImplementation}"/>,
        ///     <see cref="IDependencyCollection.BuildGraph"/> does not need to be called after registering an instance.
        /// </summary>
        /// <typeparam name="TInterface">The type that will be resolvable.</typeparam>
        /// <param name="implementation">The existing instance to use as the implementation.</param>
        /// <param name="overwrite">
        /// If true, do not throw an <see cref="InvalidOperationException"/> if an interface is already registered,
        /// replace the current implementation instead.
        /// </param>
        void RegisterInstance<TInterface>(object implementation, bool overwrite = false);

        /// <summary>
        ///     Registers an interface to an existing instance of an implementation,
        ///     making it accessible to <see cref="IDependencyCollection.Resolve{T}"/>.
        ///     Unlike <see cref="IDependencyCollection.Register{TInterface, TImplementation}"/>,
        ///     <see cref="IDependencyCollection.BuildGraph"/> does not need to be called after registering an instance.
        /// </summary>
        /// <param name="type">The type that will be resolvable.</param>
        /// <param name="implementation">The existing instance to use as the implementation.</param>
        /// <param name="overwrite">
        /// If true, do not throw an <see cref="InvalidOperationException"/> if an interface is already registered,
        /// replace the current implementation instead.
        /// </param>
        void RegisterInstance(Type type, object implementation, bool overwrite = false);

        /// <summary>
        /// Clear all services and types.
        /// Use this between unit tests and on program shutdown.
        /// If a service implements <see cref="IDisposable"/>, <see cref="IDisposable.Dispose"/> will be called on it.
        /// </summary>
        void Clear();

        /// <summary>
        /// Resolve a dependency manually.
        /// </summary>
        /// <exception cref="UnregisteredTypeException">Thrown if the interface is not registered.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the resolved type hasn't been created yet
        /// because the object graph still needs to be constructed for it.
        /// </exception>
        [Pure]
        T Resolve<T>();

        /// <summary>
        /// Resolve a dependency manually.
        /// </summary>
        /// <exception cref="UnregisteredTypeException">Thrown if the interface is not registered.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the resolved type hasn't been created yet
        /// because the object graph still needs to be constructed for it.
        /// </exception>
        [Pure]
        object ResolveType(Type type);

        /// <summary>
        /// Resolve a dependency manually.
        /// </summary>
        bool TryResolveType<T>([NotNullWhen(true)] out T? instance);

        /// <summary>
        /// Resolve a dependency manually.
        /// </summary>
        bool TryResolveType(Type objectType, [MaybeNullWhen(false)] out object instance);

        /// <summary>
        /// Initializes the object graph by building every object and resolving all dependencies.
        /// </summary>
        /// <seealso cref="DependencyCollection.InjectDependencies"/>
        void BuildGraph();

        /// <summary>
        ///     Injects dependencies into all fields with <see cref="DependencyAttribute"/> on the provided object.
        ///     This is useful for objects that are not IoC created, and want to avoid tons of IoC.Resolve() calls.
        /// </summary>
        /// <remarks>
        ///     This does NOT initialize IPostInjectInit objects!
        /// </remarks>
        /// <param name="obj">The object to inject into.</param>
        /// <param name="oneOff">If true, this object type is not expected to be injected commonly.</param>
        /// <exception cref="UnregisteredDependencyException">
        ///     Thrown if a dependency field on the object is not registered.
        /// </exception>
        /// <seealso cref="DependencyCollection.BuildGraph"/>
        void InjectDependencies(object obj, bool oneOff=false);
    }
}
