using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameObject.Exceptions;
using GameObject.System;
namespace GameObject
{
    public class EntitySystemManager
    {
        private EntityManager _entityManager;
        private Dictionary<Type, EntitySystem> _systems = new Dictionary<Type, EntitySystem>();
        private List<Type> _systemTypes;

        private bool _initialized = false;
        private bool _shutdown = false;

        public EntitySystemManager(EntityManager em)
        {
            _entityManager = em;
            _systemTypes = new List<Type>();
            switch(em.EngineType)
            {
                case EngineType.Client:
                    break;
                case EngineType.Server:
                    _systemTypes.AddRange(Assembly.LoadFrom("ServerGameComponent.dll").GetTypes().Where(t => typeof(EntitySystem).IsAssignableFrom(t)));
                    break;

            }
            foreach(var type in _systemTypes)
            {
                if (type == typeof(EntitySystem))
                    continue; //Don't run the base EntitySystem.
                //Force initialization of all systems
                var instance = Activator.CreateInstance(type, em);
                var generic = typeof (EntitySystemManager).GetMethod("AddSystem").MakeGenericMethod(type);
                generic.Invoke(this, new []{instance});
            }
        }

        public T GetEntitySystem<T>() where T : EntitySystem
        {
            var type = typeof(T);
            if (!_systems.ContainsKey(type))
            {
                throw new MissingImplementationException(type);
            }

            return (T)_systems[type];
        }

        public void Initialize()
        {
            foreach (var system in _systems.Values)
                system.Initialize();
            _initialized = true;
        }

        public void AddSystem<T>(T system) where T : EntitySystem
        {
            if(_systems.ContainsKey(typeof(T)))
            {
                RemoveSystem(system);
            }

            _systems.Add(typeof(T), system);
        }

        public void RemoveSystem<T>(T system) where T : EntitySystem
        {
            if (_systems.ContainsKey(typeof(T)))
            {
                _systems[typeof(T)].Shutdown();
                _systems.Remove(typeof(T));
            }
        }

        public void Shutdown()
        {
            foreach(var system in _systems)
            {
                RemoveSystem(system.Value);
            }
            _shutdown = true;
        }

        public void Update(float frameTime)
        {
            foreach(var system in _systems.Values)
            {
                system.Update(frameTime);
            }
        }
    }
}
