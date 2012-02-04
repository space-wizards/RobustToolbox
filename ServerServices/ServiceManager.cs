using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS13_Shared;
using ServerInterfaces;

namespace ServerServices
{
    /// <summary>
    /// WTF is wrong with me im gay
    /// </summary>
    public class ServiceManager
    {
        Dictionary<ServerServiceType, IService> services;

        private static ServiceManager singleton;
        public static ServiceManager Singleton
        {
            get {
                if (singleton == null)
                    singleton = new ServiceManager();
                return singleton;
            }
        }

        public ServiceManager()
        {
            services = new Dictionary<ServerServiceType, IService>();
        }

        public void AddService(IService service)
        {
            if (services.ContainsKey(service.ServiceType))
                return;
            else
                services.Add(service.ServiceType, service);
        }

        public void RemoveService(IService service)
        {
            if(services.ContainsKey(service.ServiceType))
                services.Remove(service.ServiceType);
        }

        public IService GetService(ServerServiceType serviceType)
        {
            if (services.ContainsKey(serviceType))
                return services[serviceType];
            else
                return null;
        }
    }
}
