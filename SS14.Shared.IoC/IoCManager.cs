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
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();
        private static readonly List<Type> ServiceTypes = new List<Type>();
        private static readonly Dictionary<Type, Type> ResolveTypes = new Dictionary<Type, Type>();

        public static void AddAssemblies(IEnumerable<Assembly> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                ServiceTypes.AddRange(assembly.GetTypes());
            }

            SortTypes();
        }

        /// <summary>
        /// Sort all resolvable types.
        /// Existing services do not get updated.
        /// </summary>
        public static void SortTypes()
        {
            ResolveTypes.Clear();
            // Cache of interface = last resolved priority to make sorting easier.
            // Yeah I could sort with LINQ but fuck that.
            var resolvedPriorities = new Dictionary<Type, int>();

            // TODO: handle types with multiple implemented interfaces in a special way?
            foreach (var type in ServiceTypes)
            {
                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (interfaceType.GetInterfaces().Any((Type t) => t == typeof(IIoCInterface)))
                    {
                        int priority = 0;
                        var attribute = (IoCTargetAttribute) Attribute.GetCustomAttribute(type, typeof(IoCTargetAttribute));
                        if (attribute != null)
                        {
                            if (attribute.Disabled)
                            {
                                break;
                            }

                            priority = attribute.Priority;
                        }

                        if (resolvedPriorities.ContainsKey(interfaceType) && resolvedPriorities[interfaceType] >= priority)
                        {
                            continue;
                        }

                        resolvedPriorities[interfaceType] = priority;
                        ResolveTypes[interfaceType] = type;
                    }
                }
            }
        }

        public static T Resolve<T>() where T: IIoCInterface
        {
            Type type = typeof(T);
            if (!Services.ContainsKey(type))
            {
                BuildType(type);
            }

            return (T)Services[type];
        }

        public static Type ResolveType<T>() where T: IIoCInterface
        {
            return ResolveType(typeof(T));
        }

        /// <summary>
        /// Resolves a type without compile time enforcement.
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

        public static T NewType<T>(params object[] args) where T: IIoCInterface
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
            if (concreteType == null) throw new MissingImplementationException(type);

            ConstructorInfo constructor = concreteType.GetConstructors().FirstOrDefault();
            if (constructor == null) throw new NoPublicConstructorException(concreteType);

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
                object instance = Activator.CreateInstance(concreteType, requiredParameters.ToArray());
                Services.Add(type, instance);
            }
            else
            {
                object instance = Activator.CreateInstance(concreteType);
                Services.Add(type, instance);
            }
        }
    }
}
