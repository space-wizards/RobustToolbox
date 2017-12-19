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
            var InterfaceType = typeof(TInterface);
            if (ResolveTypes.ContainsKey(InterfaceType))
            {
                if (!overwrite)
                {
                    throw new InvalidOperationException
                    (
                        string.Format("Attempted to register already registered interface {0}. New implementation: {1}, Old implementation: {2}",
                        InterfaceType, typeof(TImplementation), ResolveTypes[InterfaceType]
                    ));
                }

                if (Services.ContainsKey(InterfaceType))
                {
                    throw new InvalidOperationException($"Attempted to overwrite already instantiated interface {InterfaceType}.");
                }
            }

            ResolveTypes[InterfaceType] = typeof(TImplementation);
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
        /// <seealso cref="InjectDependencies(object)"/>
        public static void BuildGraph()
        {
            // List of all objects we need to inject dependencies into.
            var InjectList = new List<object>();

            // First we build every type we have registered but isn't yet built.
            // This allows us to run this after the content assembly has been loaded.
            foreach (KeyValuePair<Type, Type> currentType in ResolveTypes.Where(p => !Services.ContainsKey(p.Key)))
            {
                // Find a potential dupe by checking other registered types that have already been instantiated that have the same instance type.
                // Can't catch ourselves because we're not instantiated.
                // Ones that aren't yet instantiated are about to be and'll find us instead.
                KeyValuePair<Type, Type> DupeType = ResolveTypes.FirstOrDefault(p => Services.ContainsKey(p.Key) && p.Value == currentType.Value);

                // Interface key can't be null so since KeyValuePair<> is a struct,
                // this effectively checks whether we found something.
                if (DupeType.Key != null)
                {
                    // We have something with the same instance type, use that.
                    Services[currentType.Key] = Services[DupeType.Key];
                    continue;
                }

                try
                {
                    var instance = Activator.CreateInstance(currentType.Value);
                    Services[currentType.Key] = instance;
                    InjectList.Add(instance);
                }
                catch (TargetInvocationException e)
                {
                    throw new ImplementationConstructorException(currentType.Value, e.InnerException);
                }
            }

            // Graph built, go over ones that need injection.
            foreach (var Implementation in InjectList)
            {
                InjectDependencies(Implementation);
            }

            foreach (IPostInjectInit InjectedItem in InjectList.OfType<IPostInjectInit>())
            {
                InjectedItem.PostInject();
            }
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
        public static void InjectDependencies(object obj)
        {
            foreach (FieldInfo field in GetAllFields(obj.GetType())
                            .Where(p => Attribute.GetCustomAttribute(p, typeof(DependencyAttribute)) != null))
            {
                // Not using Resolve<T>() because we're literally building it right now.
                if (!Services.ContainsKey(field.FieldType))
                {
                    throw new UnregisteredDependencyException(obj.GetType(), field.FieldType, field.Name);
                }

                // Quick note: this DOES work with readonly fields, though it may be a CLR implementation detail.
                field.SetValue(obj, Services[field.FieldType]);
            }
        }

        /// <summary>
        /// Returns absolutely all fields, privates, readonlies, and ones from parents.
        /// </summary>
        private static IEnumerable<FieldInfo> GetAllFields(Type t)
        {
            // We need to fetch the entire class hierarchy and SelectMany(),
            // Because BindingFlags.FlattenHierarchy doesn't read privates,
            // Even when you pass BindingFlags.NonPublic.
            return GetClassHierarchy(t).SelectMany(p => p.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public));
        }

        private static IEnumerable<Type> GetClassHierarchy(Type t)
        {
            yield return t;

            while (t.BaseType != null)
            {
                t = t.BaseType;
                yield return t;
            }
        }
    }
}
