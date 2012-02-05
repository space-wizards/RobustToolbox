using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces;
using ClientServices.Exceptions;

namespace ClientServices
{
    public class ServiceManager : IServiceManager
    {
        private readonly Dictionary<Type, IService> _services;

        private static ServiceManager _singleton;
        public static ServiceManager Singleton
        {
            get { return _singleton ?? (_singleton = new ServiceManager()); }
        }

        public ServiceManager()
        {
            _services = new Dictionary<Type, IService>();
        }

        public void Register<T>() where T : IService
        {
            if (_services.ContainsKey(typeof(T)))
            {
                throw new ExistingClientServiceException(typeof(T));
            }

            var service = (T)Activator.CreateInstance(typeof(T), new object[] { this });

            _services.Add(typeof(T), service);
        }

        public void Unregister<T>() where T : IService
        {
            if (_services.ContainsKey(typeof(T)))
            {
                _services.Remove(typeof(T));
            }
        }

        public T GetService<T>()
        {
            if (!_services.ContainsKey(typeof(T)))
            {
                throw new UnregisteredClientServiceException(typeof (T));
            }

            return (T) _services[typeof (T)];
        }

        /// <summary>
        /// Custom function for retrieving the UiManager directly.
        /// Required currently due to UiManager requirement in some
        /// GOCs. The GOCs assembly references the ClientServices and
        /// ClientInterfaces, but not the Client where the UiManager
        /// resides. Referencing the Client would cause a circular 
        /// dependecy. Need to consider moving UiManager and related
        /// GuiComponents to ClientServices to clean this up further.
        /// </summary>
        /// <returns></returns>
        public IUserInterfaceManager GetUiManager()
        {
            return _services.Values.OfType<IUserInterfaceManager>().FirstOrDefault();
        }

        public void Update()
        {
            foreach (var service in _services.Values.OfType<IUpdates>())
            {
                service.Update();
            }
        }

        public void Render()
        {
            foreach (var service in _services.Values.OfType<IRenders>())
            {
                service.Render();
            }
        }
    }
}
