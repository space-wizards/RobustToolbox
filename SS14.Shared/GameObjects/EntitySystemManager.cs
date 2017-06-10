using Lidgren.Network;
using NetSerializer;
using SS14.Shared.GameObjects.Exceptions;
using SS14.Shared.GameObjects.System;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SS14.Shared.GameObjects
{
    [IoCTarget]
    public class EntitySystemManager : IEntitySystemManager
    {
        private readonly Dictionary<Type, IEntitySystem> _systems = new Dictionary<Type, IEntitySystem>();
        private readonly Dictionary<Type, IEntitySystem> _systemMessageTypes = new Dictionary<Type, IEntitySystem>();
        private EntityManager _entityManager;

        public void RegisterMessageType<T>(IEntitySystem regSystem) where T : EntitySystemMessage
        {
            Type type = typeof(T);

            if (!_systems.ContainsValue(regSystem))
            {
                throw new InvalidEntitySystemException();
            }

            if (_systemMessageTypes.ContainsKey(type)) return;

            if (!_systemMessageTypes.Any(x => x.Key == type && x.Value == regSystem))
                _systemMessageTypes.Add(type, regSystem);
        }

        public T GetEntitySystem<T>() where T : IEntitySystem
        {
            Type type = typeof(T);
            if (!_systems.ContainsKey(type))
            {
                throw new MissingImplementationException(type);
            }

            return (T)_systems[type];
        }

        public void Initialize()
        {
            foreach (Type type in IoCManager.ResolveEnumerable<IEntitySystem>())
            {
                //Force initialization of all systems
                var instance = (IEntitySystem)Activator.CreateInstance(type);
                AddSystem(instance);
                instance.RegisterMessageTypes();
                instance.SubscribeEvents();
            }
            foreach (IEntitySystem system in _systems.Values)
                system.Initialize();
        }

        private void AddSystem(IEntitySystem system)
        {
            var type = system.GetType();
            if (_systems.ContainsKey(type))
            {
                RemoveSystem(system);
            }

            _systems.Add(type, system);
        }

        private void RemoveSystem(IEntitySystem system)
        {
            var type = system.GetType();
            if (_systems.ContainsKey(type))
            {
                _systems[type].Shutdown();
                _systems.Remove(type);
            }
        }

        public void Shutdown()
        {
            foreach (var system in _systems.Values)
            {
                RemoveSystem(system);
            }
        }

        public void HandleSystemMessage(EntitySystemData sysMsg)
        {
            int messageLength = sysMsg.message.ReadInt32();

            object deserialized = Serializer.Deserialize(new MemoryStream(sysMsg.message.ReadBytes(messageLength)));

            if (deserialized is EntitySystemMessage)
                foreach (KeyValuePair<Type, IEntitySystem> current in _systemMessageTypes.Where(x => x.Key == deserialized.GetType()))
                    current.Value.HandleNetMessage((EntitySystemMessage)deserialized);
        }

        public void Update(float frameTime)
        {
            foreach (IEntitySystem system in _systems.Values)
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
    { }
}
