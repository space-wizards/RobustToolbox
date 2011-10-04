using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClientInterfaces;
using SS3D_shared;

namespace ClientServices
{
    /// <summary>
    /// WTF is wrong with me im gay
    /// </summary>
    public class ServiceManager
    {
        Dictionary<ClientServiceType, IService> services;

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
            services = new Dictionary<ClientServiceType, IService>();
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

        public IService GetService(ClientServiceType serviceType)
        {
            if (services.ContainsKey(serviceType))
                return services[serviceType];
            else
                return null;
        }
    }
}
