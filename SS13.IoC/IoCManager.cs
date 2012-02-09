using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SS13.IoC.Exceptions;

namespace SS13.IoC
{
    public static class IoCManager
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();
        private static readonly List<Type> ServiceTypes;

        static IoCManager()
        {
            ServiceTypes = new List<Type>();
            ServiceTypes.AddRange(Assembly.LoadFrom("ClientServices.dll").GetTypes());

        }

        public static T Resolve<T>()
        {
            var type = typeof(T);
            if (!Services.ContainsKey(typeof(T)))
            {
                BuildType(type);
            }

            return (T)Services[typeof(T)];
        }

        private static void BuildType(Type type)
        {
            var concreteType = ServiceTypes.FirstOrDefault(x => x.GetInterfaces().Contains(type));
            if (concreteType == null) throw new MissingImplementationException(type);

            var constructor = concreteType.GetConstructors().FirstOrDefault();
            if (constructor == null) throw new NoPublicConstructorException(concreteType);

            var parameters = constructor.GetParameters();
            if (parameters.Any())
            {
                var requiredParameters = new List<object>();
                foreach (var parameterInfo in parameters)
                {
                    if (!Services.ContainsKey(parameterInfo.ParameterType))
                    {
                        BuildType(parameterInfo.ParameterType);
                    }

                    var dependency = Services[parameterInfo.ParameterType];
                    requiredParameters.Add(dependency);
                }
                var instance = Activator.CreateInstance(concreteType, requiredParameters.ToArray());
                Services.Add(type, instance);
            }
            else
            {
                var instance = Activator.CreateInstance(concreteType);
                Services.Add(type, instance);
            }
        }
    }
}
