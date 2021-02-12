using Robust.Shared.IoC.Exceptions;
using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Reflection;
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
    /// unless they have first been initialized with <see cref="InitThread()"/> or <see cref="InitThread(IDependencyCollection,bool)"/>.
    /// You should not initialize IoC in thread pools like that of <see cref="Task.Run(Action)"/>,
    /// since said thread pool might be used by different running instances
    /// (for example, server and client running in the same process, they have a different IoC instance).
    /// </para>
    /// </remarks>
    /// <seealso cref="IReflectionManager"/>
    public static class IoCManager
    {
        private const string NoContextAssert = "IoC has no context on this thread. Are you calling IoC from the wrong thread or did you forget to initialize it?";
        private static readonly ThreadLocal<IDependencyCollection> _container = new();

        /// <summary>
        /// Returns the singleton thread-local instance of the IoCManager's dependency collection.
        /// </summary>
        /// <remarks>
        /// This property will be null if <see cref="InitThread()"/> has not been called on this thread yet.
        /// </remarks>
        public static IDependencyCollection? Instance => _container.IsValueCreated ? _container.Value : null;

        /// <summary>
        /// Ensures that the <see cref="IDependencyCollection"/> instance exists for this thread.
        /// </summary>
        /// <remarks>
        /// This will create a new instance of a <see cref="IDependencyCollection"/> for this thread,
        /// otherwise it will do nothing if one already exists.
        /// </remarks>
        public static void InitThread()
        {
            if (_container.IsValueCreated)
            {
                return;
            }

            _container.Value = new DependencyCollection();
        }

        /// <summary>
        /// Sets an existing <see cref="IDependencyCollection"/> as the instance for this thread.
        /// </summary>
        /// <exception cref="InvalidOperationException">Will be thrown if a <see cref="IDependencyCollection"/> instance is already set for this thread,
        /// and replaceExisting is set to false.</exception>
        /// <param name="collection">Collection to set as the instance for this thread.</param>
        /// <param name="replaceExisting">If this is true, replaces the existing collection, if one is set for this thread.</param>
        public static void InitThread(IDependencyCollection collection, bool replaceExisting=false)
        {
            if (_container.IsValueCreated && !replaceExisting)
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

            _container.Value!.Register<TInterface, TImplementation>(overwrite);
        }

        /// <summary>
        /// Registers an interface to an implementation, to make it accessible to <see cref="Resolve{T}"/>
        /// <see cref="BuildGraph"/> MUST be called after this method to make the new interface available.
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
        /// or if an already instantiated interface (by <see cref="BuildGraph"/>) is attempting to be overwritten.
        /// </exception>
        public static void Register<TInterface, TImplementation>(DependencyFactoryDelegate<TImplementation> factory, bool overwrite = false)
            where TImplementation : class, TInterface
        {
            DebugTools.Assert(_container.IsValueCreated, NoContextAssert);

            _container.Value!.Register<TInterface, TImplementation>(factory, overwrite);
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

            _container.Value!.RegisterInstance<TInterface>(implementation, overwrite);
        }

        /// <summary>
        /// Clear all services and types.
        /// Use this between unit tests and on program shutdown.
        /// If a service implements <see cref="IDisposable"/>, <see cref="IDisposable.Dispose"/> will be called on it.
        /// </summary>
        public static void Clear()
        {
            if (_container.IsValueCreated)
                _container.Value!.Clear();
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

            return _container.Value!.Resolve<T>();
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

            return _container.Value!.ResolveType(type);
        }

        /// <summary>
        /// Initializes the object graph by building every object and resolving all dependencies.
        /// </summary>
        /// <seealso cref="InjectDependencies{T}"/>
        public static void BuildGraph()
        {
            DebugTools.Assert(_container.IsValueCreated, NoContextAssert);

            _container.Value!.BuildGraph();
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
        public static T InjectDependencies<T>(T obj) where T : notnull
        {
            DebugTools.Assert(_container.IsValueCreated, NoContextAssert);
            _container.Value!.InjectDependencies(obj);
            return obj;
        }
    }
}
