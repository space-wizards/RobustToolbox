using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.System;
using SS14.Shared.Interfaces.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.Network.Messages;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Shared.GameObjects
{
    public class EntitySystemManager : IEntitySystemManager
    {
        [Dependency]
        private readonly IReflectionManager ReflectionManager;
        /// <summary>
        /// Maps system types to instances.
        /// </summary>
        private readonly Dictionary<Type, IEntitySystem> Systems = new Dictionary<Type, IEntitySystem>();

        /// <summary>
        /// Maps child types of <see cref="EntitySystemMessage"/> to the system that will be receiving them.
        /// </summary>
        private readonly Dictionary<Type, IEntitySystem> SystemMessageTypes = new Dictionary<Type, IEntitySystem>();

        /// <exception cref="InvalidOperationException">Thrown if the specified type is already registered by another system.</exception>
        /// <exception cref="InvalidEntitySystemException">Thrown if the entity system instance is not registered with this <see cref="EntitySystemManager"/></exception>
        /// <exception cref="ArgumentNullException">Thrown if the provided system is null.</exception>
        public void RegisterMessageType<T>(IEntitySystem regSystem) where T : EntitySystemMessage
        {
            if (regSystem == null)
            {
                throw new ArgumentNullException(nameof(regSystem));
            }
            Type type = typeof(T);

            if (!Systems.ContainsValue(regSystem))
            {
                throw new InvalidEntitySystemException();
            }

            if (SystemMessageTypes.ContainsKey(type))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Duplicate EntitySystemMessage registration: {0} is already registered by {1}.",
                        type,
                        SystemMessageTypes[type].GetType()));
            }

            SystemMessageTypes.Add(type, regSystem);
        }

        /// <exception cref="InvalidEntitySystemException">Thrown if the provided type is not registered.</exception>
        public T GetEntitySystem<T>() where T : IEntitySystem
        {
            Type type = typeof(T);
            if (!Systems.ContainsKey(type))
            {
                throw new InvalidEntitySystemException();
            }

            return (T)Systems[type];
        }

        public void Initialize()
        {
            foreach (Type type in ReflectionManager.GetAllChildren<IEntitySystem>())
            {
                //Force initialization of all systems
                var instance = (IEntitySystem)Activator.CreateInstance(type);
                AddSystem(instance);
                instance.RegisterMessageTypes();
                instance.SubscribeEvents();
            }
            foreach (IEntitySystem system in Systems.Values)
                system.Initialize();
        }

        private void AddSystem(IEntitySystem system)
        {
            var type = system.GetType();
            if (Systems.ContainsKey(type))
            {
                RemoveSystem(system);
            }

            Systems.Add(type, system);
        }

        private void RemoveSystem(IEntitySystem system)
        {
            var type = system.GetType();
            if (Systems.ContainsKey(type))
            {
                Systems[type].Shutdown();
                Systems.Remove(type);
            }
        }

        public void Shutdown()
        {
            // System.Values is modified by RemoveSystem
            var values = Systems.Values.ToArray();
            foreach (var system in values)
            {
                RemoveSystem(system);
            }
        }

        public void HandleSystemMessage(MsgEntity sysMsg)
        {
            foreach (var current in SystemMessageTypes.Where(x => x.Key == sysMsg.SystemMessage.GetType()))
            {
                current.Value.HandleNetMessage(sysMsg.MsgChannel, sysMsg.SystemMessage);
            }
        }

        public void Update(float frameTime)
        {
            foreach (IEntitySystem system in Systems.Values)
            {
                system.Update(frameTime);
            }
        }

        public void FrameUpdate(float frameTime)
        {
            foreach (IEntitySystem system in Systems.Values)
            {
                system.FrameUpdate(frameTime);
            }
        }
    }

    public class InvalidEntitySystemException : Exception
    { }
}
