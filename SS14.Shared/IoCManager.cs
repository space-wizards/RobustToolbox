using SS14.Shared.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SS14.Shared
{
    public static class IoCManager
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();
        private static readonly List<Type> ServiceTypes;

        static IoCManager()
        {
            ServiceTypes = new List<Type>();
            if (Assembly.GetEntryAssembly().GetName().Name == "SpaceStation14")
                ServiceTypes.AddRange(Assembly.LoadFrom("SS14.Client.Services.dll").GetTypes());
            else if (Assembly.GetEntryAssembly().GetName().Name == "SpaceStation14_Server")
                ServiceTypes.AddRange(Assembly.LoadFrom("SS14.Server.Services.dll").GetTypes());
        }

        public static T Resolve<T>()
        {
            Type type = typeof (T);
            if (!Services.ContainsKey(typeof (T)))
            {
                BuildType(type);
            }

            return (T) Services[typeof (T)];
        }

        private static void BuildType(Type type)
        {
            Type concreteType = ServiceTypes.FirstOrDefault(x => x.GetInterfaces().Contains(type));
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