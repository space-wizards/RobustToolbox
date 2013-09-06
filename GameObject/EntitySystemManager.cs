using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GameObject.Exceptions;
using GameObject.System;
using Lidgren.Network;
using NetSerializer;
using SS13_Shared.GO;
using SS13_Shared.Serialization;

namespace GameObject
{
    public class EntitySystemManager
    {
        private readonly List<Type> _systemTypes;
        private readonly Dictionary<Type, EntitySystem> _systems = new Dictionary<Type, EntitySystem>();
        private readonly Dictionary<string, Type> _systemStrings = new Dictionary<string, Type>();
        private EntityManager _entityManager;

        private bool _initialized;
        private bool _shutdown;

        public EntitySystemManager(EntityManager em)
        {
            _entityManager = em;
            _systemTypes = new List<Type>();
            switch (em.EngineType)
            {
                case EngineType.Client:
                    _systemTypes.AddRange(
                        Assembly.LoadFrom("CGO.dll").GetTypes().Where(
                            t => typeof(EntitySystem).IsAssignableFrom(t)));
                    break;
                case EngineType.Server:
                    _systemTypes.AddRange(
                        Assembly.LoadFrom("ServerGameComponent.dll").GetTypes().Where(
                            t => typeof (EntitySystem).IsAssignableFrom(t)));
                    break;
            }
            foreach (Type type in _systemTypes)
            {
                if (type == typeof (EntitySystem))
                    continue; //Don't run the base EntitySystem.
                //Force initialization of all systems
                object instance = Activator.CreateInstance(type, em);
                MethodInfo generic = typeof (EntitySystemManager).GetMethod("AddSystem").MakeGenericMethod(type);
                generic.Invoke(this, new[] {instance});

                _systemStrings.Add(type.Name, type);
            }

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
            string targetSystemStr = sysMsg.message.ReadString();
            if (!_systemStrings.ContainsKey(targetSystemStr)) return;
            Type targetSystemType = _systemStrings[targetSystemStr];

            if (targetSystemType == null) throw new NullReferenceException("Invalid Entity System Type specified for Entity System Message.");

            ArrayList byteList = new ArrayList();

            while (sysMsg.message.Position < sysMsg.message.LengthBits)
                byteList.Add(sysMsg.message.ReadByte());

            object deserialized = Serializer.Deserialize(new MemoryStream((byte[])byteList.ToArray(typeof(byte)))); //Fuck microsoft.

            if (deserialized is EntitySystemMessage) //No idea if this works.
            {
                foreach (KeyValuePair<Type, EntitySystem> curr in _systems)
                    if (curr.Key == targetSystemType || targetSystemType.IsAssignableFrom(curr.Key)) //Send to systems of same type and systems derived from this type. This is needed for some thing i had in mind (the sending to derived classes).
                        curr.Value.HandleNetMessage((EntitySystemMessage)deserialized);
            }
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
        public int sourceEntityUid;
        public NetIncomingMessage message;
        public NetConnection senderConnection;

        public EntitySystemData(int sourceEntityUid, NetConnection senderConnection, NetIncomingMessage message)
        {
            this.sourceEntityUid = sourceEntityUid;
            this.senderConnection = senderConnection;
            this.message = message;
        }
    }
}