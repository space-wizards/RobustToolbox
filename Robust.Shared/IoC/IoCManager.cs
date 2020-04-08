using Robust.Shared.IoC.Exceptions;
using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Utility;

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
    /// <para>
    /// <c>IoCManager</c> is actually a static wrapper class around a thread local <see cref="IDependencyCollection"/>.
    /// As such, <c>IoCManager</c> will not work in other threads,
    /// unless they have first been initialized with <see cref="InitThread(Robust.Shared.IoC.IDependencyCollection)"/>.
    /// You should not initialize IoC in thread pools like that of <see cref="Task.Run(Action)"/>,
    /// since said thread pool might be used by different running instances
    /// (for example, server and client running in the same process, they have a different IoC instance).
    /// </para>
    /// </remarks>
    /// <seealso cref="Interfaces.Reflection.IReflectionManager"/>
    public static class IoCManager
    {
        private const string NoContextAssert = "IoC has no context on this thread. Are you calling IoC from the wrong thread or did you forget to initialize it?";
        private static readonly ThreadLocal<IDependencyCollection> _container = new ThreadLocal<IDependencyCollection>();

        public static void InitThread()
        {
            if (_container.IsValueCreated)
            {
                return;
            }

            _container.Value = new DependencyCollection();
        }

        public static void InitThread(IDependencyCollection collection)
        {
            if (_container.IsValueCreated)
            {
                throw new InvalidOperationException("This thread has already been initialized.");
            }

            _container.Value = collection;
        }

        /// <summary>
        /// Registers an interface to an implementation, to make it accessible to <see cref="Resolve{T}"/>
        /// </summary>
        /// <typeparam name="TInterface">The type that will be resolvable.</typeparam>
        /// <typeparam name="TImplementation">The type that will be constructed as implementation.</typeparam>
        /// <param name="overwrite">
        /// If true, do not throw an <see cref="InvalidOperationException"/> if an interface is already registered,
        /// replace the current implementation instead.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <paramref name="overwrite"/> is false and <typeparamref name="TInterface"/> has been registered before,
        /// or if an already instantiated interface (by <see cref="BuildGraph"/>) is attempting to be overwritten.
        /// </exception>
        public static void Register<TInterface, TImplementation>(bool overwrite = false)
            where TImplementation : class, TInterface, new()
        {
            DebugTools.Assert(_container.IsValueCreated, NoContextAssert);

            _container.Value.Register<TInterface, TImplementation>(overwrite);
        }

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
        public static void RegisterInstance<TInterface>(object implementation, bool overwrite = false)
        {
            DebugTools.Assert(_container.IsValueCreated, NoContextAssert);

            _container.Value.RegisterInstance<TInterface>(implementation, overwrite);
        }

        /// <summary>
        /// Clear all services and types.
        /// Use this between unit tests and on program shutdown.
        /// If a service implements <see cref="IDisposable"/>, <see cref="IDisposable.Dispose"/> will be called on it.
        /// </summary>
        public static void Clear()
        {
            if (_container.IsValueCreated)
                _container.Value.Clear();
        }

        /// <summary>
        /// Resolve a dependency manually.
        /// </summary>
        /// <exception cref="UnregisteredTypeException">Thrown if the interface is not registered.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the resolved type hasn't been created yet
        /// because the object graph still needs to be constructed for it.
        /// </exception>
        [Pure]
        public static T Resolve<T>()
        {
            DebugTools.Assert(_container.IsValueCreated, NoContextAssert);

            return _container.Value.Resolve<T>();
        }

        /// <summary>
        /// Resolve a dependency manually.
        /// </summary>
        /// <exception cref="UnregisteredTypeException">Thrown if the interface is not registered.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the resolved type hasn't been created yet
        /// because the object graph still needs to be constructed for it.
        /// </exception>
        [Pure]
        public static object ResolveType(Type type)
        {
            DebugTools.Assert(_container.IsValueCreated, NoContextAssert);

            return _container.Value.ResolveType(type);
        }

        /// <summary>
        /// Initializes the object graph by building every object and resolving all dependencies.
        /// </summary>
        /// <seealso cref="InjectDependencies(object)"/>
        public static void BuildGraph()
        {
            DebugTools.Assert(_container.IsValueCreated, NoContextAssert);

            _container.Value.BuildGraph();
        }

        /// <summary>
        ///     Injects dependencies into all fields with <see cref="DependencyAttribute"/> on the provided object.
        ///     This is useful for objects that are not IoC created, and want to avoid tons of IoC.Resolve() calls.
        /// </summary>
        /// <remarks>
        ///     This does NOT initialize IPostInjectInit objects!
        /// </remarks>
        /// <param name="obj">The object to inject into.</param>
        /// <exception cref="UnregisteredDependencyException">
        ///     Thrown if a dependency field on the object is not registered.
        /// </exception>
        /// <seealso cref="BuildGraph"/>
        public static T InjectDependencies<T>(T obj)
        {
            DebugTools.Assert(_container.IsValueCreated, NoContextAssert);
            _container.Value.InjectDependencies(obj);
            return obj;
        }
    }
}
