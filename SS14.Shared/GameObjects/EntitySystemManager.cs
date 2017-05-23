using Lidgren.Network;
using NetSerializer;
using SS14.Shared.GameObjects.Exceptions;
using SS14.Shared.GameObjects.System;
using SS14.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SS14.Shared.GameObjects
{
    public class EntitySystemManager
    {
        private readonly Dictionary<Type, EntitySystem> _systems = new Dictionary<Type, EntitySystem>();
        private readonly Dictionary<Type, EntitySystem> _systemMessageTypes = new Dictionary<Type, EntitySystem>();
        private EntityManager _entityManager;

        private bool _initialized;
        private bool _shutdown;

        public EntitySystemManager(EntityManager em)
        {
            _entityManager = em;
            IEnumerable<Type> _systemTypes = Assembly.GetEntryAssembly().GetTypes().Where(
                 t => typeof(EntitySystem).IsAssignableFrom(t));

            foreach (Type type in _systemTypes)
            {
                if (type == typeof(EntitySystem))
                    continue; //Don't run the base EntitySystem.
                //Force initialization of all systems
                var instance = (EntitySystem)Activator.CreateInstance(type, _entityManager, this);
                MethodInfo generic = typeof(EntitySystemManager).GetMethod("AddSystem").MakeGenericMethod(type);
                generic.Invoke(this, new[] { instance });
                instance.RegisterMessageTypes();
                instance.SubscribeEvents();

            }
        }

        public void RegisterMessageType<T>(EntitySystem regSystem) where T : EntitySystemMessage
        {
            Type type = typeof(T);

            if (!_systems.ContainsValue(regSystem))
            {
                throw new InvalidEntitySystemException();
            }

            if (_systemMessageTypes.ContainsKey(type)) return;

            if(!_systemMessageTypes.Any(x => x.Key == type && x.Value == regSystem))
                _systemMessageTypes.Add(type, regSystem);
        }

        public T GetEntitySystem<T>() where T : EntitySystem
        {
            Type type = typeof (T);
            if (!_systems.ContainsKey(type))
            {
                throw new MissingImplementationException(type);
            }

            return (T) _systems[type];
        }

        public void Initialize()
        {
            foreach (EntitySystem system in _systems.Values)
                system.Initialize();
            _initialized = true;
        }

        public void AddSystem<T>(T system) where T : EntitySystem
        {
            if (_systems.ContainsKey(typeof (T)))
            {
                RemoveSystem(system);
            }

            _systems.Add(typeof (T), system);
        }

        public void RemoveSystem<T>(T system) where T : EntitySystem
        {
            if (_systems.ContainsKey(typeof (T)))
            {
                _systems[typeof (T)].Shutdown();
                _systems.Remove(typeof (T));
            }
        }

        public void Shutdown()
        {
            foreach (var system in _systems)
            {
                RemoveSystem(system.Value);
            }
            _shutdown = true;
        }

        public void HandleSystemMessage(EntitySystemData sysMsg)
        {
            Int32 messageLength = sysMsg.message.ReadInt32();

            object deserialized = Serializer.Deserialize(new MemoryStream(sysMsg.message.ReadBytes(messageLength)));

            if (deserialized is EntitySystemMessage)
                foreach (KeyValuePair<Type, EntitySystem> current in _systemMessageTypes.Where(x => x.Key == deserialized.GetType()))
                    current.Value.HandleNetMessage((EntitySystemMessage)deserialized);
        }

        public void Update(float frameTime)
        {
            foreach (EntitySystem system in _systems.Values)
            {
                system.Update(frameTime);
            }
        }
    }

    public struct EntitySystemData
    {
        public NetIncomingMessage message;
        public NetConnection senderConnection;

        public EntitySystemData(NetConnection senderConnection, NetIncomingMessage message)
        {
            this.senderConnection = senderConnection;
            this.message = message;
        }
    }

    public class InvalidEntitySystemException : Exception
    {}
}
