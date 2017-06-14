using SS14.Shared.IoC.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SS14.Shared.IoC
{
    public static class IoCManager
    {
        // Used to detect whether we have circular dependencies.
        private static readonly HashSet<Type> CurrentlyBuilding = new HashSet<Type>();
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();
        private static readonly List<Type> ServiceTypes = new List<Type>();
        private static readonly Dictionary<Type, Type> ResolveTypes = new Dictionary<Type, Type>();
        // Could probably merge these two lists but I'll hold off
        // On the basis of premature optimization.
        // Any laziness.
        private static readonly Dictionary<Type, List<Type>> ResolveListTypes = new Dictionary<Type, List<Type>>();

        public delegate void AssemblyAddedHandler();
        /// <summary>
        /// Invoked after new assemblies are loaded into IoC.
        /// Hook into this if you iterate available IoC targets for an interfaces, Like console commands.
        /// </summary>
        public static event AssemblyAddedHandler AssemblyAdded;

        /// <summary>
        /// Add extra assemblies to IoC's known types.
        /// </summary>
        public static void AddAssemblies(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                try
                {
                    ServiceTypes.AddRange(assembly.GetTypes());
                }
                catch (ReflectionTypeLoadException ex)
                {
                    throw new AggregateException(
                        string.Format("One or more exceptions occured while trying to load types from assembly {0}", assembly.FullName),
                        ex.LoaderExceptions);
                }
            }

            SortTypes();

            if (AssemblyAdded != null)
            {
                AssemblyAdded.Invoke();
            }
        }

        /// <summary>
        /// Add extra assemblies to IoC's known types.
        /// </summary>
        public static void AddAssemblies(params Assembly[] args)
        {
            AddAssemblies(args.AsEnumerable());
        }

        public static void Clear()
        {
            AssemblyAdded = null;
            Services.Clear();
            ServiceTypes.Clear();
            ResolveTypes.Clear();
            ResolveListTypes.Clear();
        }

        /// <summary>
        /// Sort all resolvable types.
        /// Existing services do not get updated.
        /// </summary>
        public static void SortTypes()
        {
            ResolveTypes.Clear();
            ResolveListTypes.Clear();
            // Cache of interface = last resolved priority to make sorting easier.
            // Yeah I could sort with LINQ but fuck that.
            var resolvedPriorities = new Dictionary<Type, int>();

            // TODO: handle types with multiple implemented interfaces in a special way?
            foreach (var type in ServiceTypes)
            {
                var attribute = (IoCTargetAttribute)Attribute.GetCustomAttribute(type, typeof(IoCTargetAttribute));
                if (attribute == null || attribute.Disabled)
                {
                    continue;
                }
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (interfaceType.GetInterfaces().Any(t => t == typeof(IIoCInterface)))
                    {
                        if (!ResolveListTypes.ContainsKey(interfaceType))
                        {
                            ResolveListTypes[interfaceType] = new List<Type>(1);
                        }

                        ResolveListTypes[interfaceType].Add(type);

                        if (resolvedPriorities.ContainsKey(interfaceType) && resolvedPriorities[interfaceType] >= attribute.Priority)
                        {
                            continue;
                        }

                        resolvedPriorities[interfaceType] = attribute.Priority;
                        ResolveTypes[interfaceType] = type;
                    }
                }
            }
        }

        /// <summary>
        /// Resolve a static instance of the highest priority implementation of an interface.
        /// see <see cref="NewType"/> if you want to resolve a NEW  instance.
        /// </summary>
        public static T Resolve<T>() where T : IIoCInterface
        {
            Type type = typeof(T);
            if (!Services.ContainsKey(type))
            {
                BuildType(type);
            }

            return (T)Services[type];
        }

        /// <summary>
        /// Resolves an IoC target's type, not a static instance.
        /// </summary>
        public static Type ResolveType<T>() where T : IIoCInterface
        {
            return ResolveType(typeof(T));
        }

        /// <summary>
        /// Resolves a target's type without compile time enforcement.
        /// Do not use this outside reflection!
        /// </summary>
        public static Type ResolveType(Type type)
        {
            if (!ResolveTypes.ContainsKey(type))
            {
                throw new MissingImplementationException(type);
            }
            return ResolveTypes[type];
        }

        /// <summary>
        /// Resolves an enumerable over all types that implement an interface, regardless of priority.
        /// The order of these types is completely arbitrary.
        /// </summary>
        public static IEnumerable<Type> ResolveEnumerable<T>() where T : IIoCInterface
        {
            var type = typeof(T);
            if (!ResolveListTypes.ContainsKey(type))
            {
                throw new MissingImplementationException(type);
            }

            return ResolveListTypes[type];
        }

        /// <summary>
        /// Resolves a type and creates a NEW instance.
        /// </summary>
        public static T NewType<T>(params object[] args) where T : IIoCInterface
        {
            return (T)Activator.CreateInstance(ResolveType<T>(), args);
        }

        /// <summary>
        /// Resolves and creates a type without compile time enforcement.
        /// Do not use this outside reflection!
        /// </summary>
        public static IIoCInterface NewType(Type type, params object[] args)
        {
            return (IIoCInterface)Activator.CreateInstance(ResolveType(type), args);
        }

        private static void BuildType(Type type)
        {
            Type concreteType = ResolveType(type);
            if (concreteType == null)
            {
                throw new MissingImplementationException(type);
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

            if (CurrentlyBuilding.Contains(concreteType))
            {
                throw new CircularDependencyException(type);
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
                    catch (Exception e)
                    {
                        throw new ImplementationConstructorException(concreteType, e);
                    }
                }
                else
                {
                    try
                    {
                        object instance = Activator.CreateInstance(concreteType);
                        Services.Add(type, instance);
                    }
                    catch (Exception e)
                    {
                        throw new ImplementationConstructorException(concreteType, e);
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
