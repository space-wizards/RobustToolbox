using SS14.Shared.IoC.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SS14.Shared.Exceptions;

namespace SS14.Shared.IoC
{
    /// <summary>
    /// The IoCManager handles Dependency Injection in the project.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Dependency Injection is a concept where instead of saying "I need the <code>EntityManager</code>",
    /// you say "I need something that implements <code>IEntityManager</code>".
    /// This decouples the various systems into swappable components that have standardized interfaces.
    /// </para>
    /// <para>
    /// This is useful for a couple of things.
    /// Firstly, it allows the shared code to request the client or server code implicitly, without hacks.
    /// Secondly, it's very useful for unit tests as we can replace components to test things.
    /// </para>
    /// <para>
    /// To use the IoCManager, it first needs some types registered through <see cref="Register{TInterface, TImplementation}"/>.
    /// These implementations can then be fetched with <see cref="Resolve{T}"/>.
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
        /// <typeparam name="TImplementation">The type that will be constructed as implementation. Must not be abstract or an interface.</typeparam>
        /// <param name="overwrite">
        /// If true, do not throw an <see cref="InvalidOperationException"/> if an interface is already registered,
        /// replace the current implementation instead.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <paramref name="overwrite"/> is false and <typeparamref name="TInterface"/> has been registered before.
        /// </exception>
        public static void Register<TInterface, TImplementation>(bool overwrite = false) where TImplementation : TInterface
        {
            if (typeof(TImplementation).IsAbstract)
            {
                throw new TypeArgumentException("Must not be abstract.", nameof(TImplementation));
            }
            var interfaceType = typeof(TInterface);
            if (!overwrite && ResolveTypes.ContainsKey(interfaceType))
            {
                throw new InvalidOperationException(
                    string.Format("Attempted to register already registered interface {0}. New implementation: {1}, Old implementation: {2}",
                                  interfaceType, typeof(TImplementation), ResolveTypes[interfaceType]
                    ));
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
        /// <exception cref="MissingImplementationException">Thrown if the interface was not registered beforehand.</exception>
        /// <exception cref="NoPublicConstructorException">Thrown if the resolved implementation does not have a public constructor.</exception>
        /// <exception cref="CircularDependencyException">Thrown if the type is already being built. This usually means a circular dependency exists.</exception>
        public static T Resolve<T>()
        {
            Type type = typeof(T);
            if (!Services.ContainsKey(type))
            {
                BuildType(type);
            }

            return (T)Services[type];
        }

        /// <summary>
        /// Build the implementation for an interface.
        /// Registers the built implementation so that it can be directly indexed.
        /// </summary>
        /// <param name="type">The interface to build.</param>
        private static void BuildType(Type type)
        {
            if (!ResolveTypes.TryGetValue(type, out Type concreteType))
            {
                throw new MissingImplementationException(type);
            }

            // We're already building this, this means circular dependency!
            if (CurrentlyBuilding.Contains(concreteType))
            {
                throw new CircularDependencyException(type);
            }

            // See if we already have an valid instance but registered as a different type.
            // Example being the EntityManager on client and server,
            // which have subinterfaces like IServerEntityManager.
            var potentialInstance = Services.Values.FirstOrDefault(s => type.IsAssignableFrom(s.GetType()));
            if (potentialInstance != null)
            {
                // NOTE: if BuildType() gets called when there already IS an instance for the type.
                // This'll "catch" that too.
                // This is NOT intended and do not rely on it.
                Services[type] = potentialInstance;
                return;
            }

            ConstructorInfo constructor = concreteType.GetConstructors().FirstOrDefault();
            if (constructor == null)
            {
                throw new NoPublicConstructorException(concreteType);
            }

            CurrentlyBuilding.Add(concreteType);

            try
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                if (parameters.Any())
                {
                    var requiredParameters = new List<object>();
                    foreach (ParameterInfo parameterInfo in parameters)
                    {
                        if (!Services.ContainsKey(parameterInfo.ParameterType))
                        {
                            BuildType(parameterInfo.ParameterType);
                        }

                        object dependency = Services[parameterInfo.ParameterType];
                        requiredParameters.Add(dependency);
                    }
                    try
                    {
                        object instance = Activator.CreateInstance(concreteType, requiredParameters.ToArray());
                        Services.Add(type, instance);
                    }
                    catch (TargetInvocationException e)
                    {
                        throw new ImplementationConstructorException(concreteType, e.InnerException);
                    }
                }
                else
                {
                    try
                    {
                        object instance = Activator.CreateInstance(concreteType);
                        Services.Add(type, instance);
                    }
                    catch (TargetInvocationException e)
                    {
                        throw new ImplementationConstructorException(concreteType, e.InnerException);
                    }
                }
            }
            finally
            {
                CurrentlyBuilding.Remove(concreteType);
            }
        }
    }
}
