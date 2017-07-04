using SS14.Shared.IoC.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SS14.Shared.IoC
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
    /// <seealso cref="Interfaces.Reflection.IReflectionManager"/>
    public static class IoCManager
    {
        /// <summary>
        /// Set of types that are currently being built by <see cref="BuildType(Type)"/>.
        /// Used to track whether we have circular dependencies.
        /// </summary>
        private static readonly HashSet<Type> CurrentlyBuilding = new HashSet<Type>();

        /// <summary>
        /// Dictionary that maps the types passed to <see cref="Resolve{T}"/> to their implementation.
        /// </summary>
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        /// <summary>
        /// The types interface types mapping to their registered implementations.
        /// This is pulled from to make a service if it doesn't exist yet.
        /// </summary>
        private static readonly Dictionary<Type, Type> ResolveTypes = new Dictionary<Type, Type>();

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
        /// or if an already instantiated interface (by <see cref="BuildGraph"/>) is attempting to be overwriten.
        /// </exception>
        public static void Register<TInterface, TImplementation>(bool overwrite = false) where TImplementation : class, TInterface, new()
        {
            var interfaceType = typeof(TInterface);
            if (ResolveTypes.ContainsKey(interfaceType))
            {
                if (!overwrite)
                {
                    throw new InvalidOperationException(
                        string.Format("Attempted to register already registered interface {0}. New implementation: {1}, Old implementation: {2}",
                        interfaceType, typeof(TImplementation), ResolveTypes[interfaceType]
                    ));
                }

                if (Services.ContainsKey(interfaceType))
                {
                    throw new InvalidOperationException($"Attempted to overwrite already instantiated interface {interfaceType}.");
                }
            }

            ResolveTypes[interfaceType] = typeof(TImplementation);
        }

        /// <summary>
        /// Clear all services and types.
        /// Use this between unit tests and on program shutdown.
        /// If a service implements <see cref="IDisposable"/>, <see cref="IDisposable.Dispose"/> will be called on it.
        /// </summary>
        public static void Clear()
        {
            foreach (var service in Services.Values.OfType<IDisposable>().Distinct())
            {
                service.Dispose();
            }
            Services.Clear();
            ResolveTypes.Clear();
        }

        /// <summary>
        /// Resolve a dependency manually.
        /// </summary>
        /// <exception cref="UnregisteredTypeException">Thrown if the interface is not registered.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the resolved type hasn't been created yet
        /// because the object graph still needs to be constructed for it.
        /// </exception>
        public static T Resolve<T>()
        {
            Type type = typeof(T);
            if (!Services.ContainsKey(type))
            {
                if (ResolveTypes.ContainsKey(type))
                {
                    // If we have the type registered but not created that means we haven't been told to initialize the graph yet.
                    throw new InvalidOperationException($"Attempted to resolve type {type} before the object graph for it has been populated.");
                }

                throw new UnregisteredTypeException(type);
            }

            return (T)Services[type];
        }

        /// <summary>
        /// Initializes the object graph by building every object and resolving all dependencies.
        /// </summary>
        public static void BuildGraph()
        {
            // List of all objects we need to inject dependencies into.
            var toInject = new List<object>();

            // First we build every type we have registered but isn't yet built.
            // This allows us to run this after the content assembly has been loaded.
            foreach (KeyValuePair<Type, Type> currentType in ResolveTypes.Where(p => !Services.ContainsKey(p.Key)))
            {
                // Find a potential dupe by checking other registered types that have already been instantiated that have the same instance type.
                // Can't catch ourselves because we're not instantiated.
                // Ones that aren't yet instantiated are about to be and'll find us instead.
                KeyValuePair<Type, Type> dupeType = ResolveTypes
                                                    .Where(p => Services.ContainsKey(p.Key) && p.Value == currentType.Value)
                                                    .FirstOrDefault();

                // Interface key can't be null so since KeyValuePair<> is a struct,
                // this effectively checks whether we found something.
                if (dupeType.Key != null)
                {
                    // We have something with the same instance type, use that.
                    Services[currentType.Key] = Services[dupeType.Key];
                    continue;
                }

                try
                {
                    var instance = Activator.CreateInstance(currentType.Value);
                    Services[currentType.Key] = instance;
                    toInject.Add(instance);
                }
                catch (TargetInvocationException e)
                {
                    throw new ImplementationConstructorException(currentType.Value, e.InnerException);
                }
            }

            // Graph built, go over ones that need injection.
            foreach (var implementation in toInject)
            {
                foreach (FieldInfo field in implementation.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    // Not using Resolve<T>() because we're literally building it right now.
                    if (!Services.ContainsKey(field.FieldType))
                    {
                        throw new UnregisteredDependencyException(implementation.GetType(), field.FieldType, field.Name);
                    }

                    // Quick note: this DOES work with readonly fields, though it may be a CLR implementation detail.
                    field.SetValue(implementation, Services[field.FieldType]);
                }
            }

            foreach (IPostInjectInit item in toInject.OfType<IPostInjectInit>())
            {
                item.PostInject();
            }
        }
    }
}
