using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Systems;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.Shared.GameObjects
{
    public class EntitySystemManager : IEntitySystemManager
    {
#pragma warning disable 649
        [Dependency] private readonly IReflectionManager _reflectionManager;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        /// <summary>
        /// Maps system types to instances.
        /// </summary>
        private readonly Dictionary<Type, IEntitySystem> _systems = new Dictionary<Type, IEntitySystem>();

        /// <exception cref="InvalidEntitySystemException">Thrown if the provided type is not registered.</exception>
        public T GetEntitySystem<T>()
            where T : IEntitySystem
        {
            var type = typeof(T);
            if (!_systems.ContainsKey(type))
            {
                throw new InvalidEntitySystemException();
            }

            return (T)_systems[type];
        }

        /// <inheritdoc />
        public bool TryGetEntitySystem<T>(out T entitySystem)
            where T : IEntitySystem
        {
            if (_systems.TryGetValue(typeof(T), out var system))
            {
                entitySystem = (T) system;
                return true;
            }

            entitySystem = default;
            return false;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            foreach (var type in _reflectionManager.GetAllChildren<IEntitySystem>())
            {
                Logger.DebugS("go.sys", "Initializing entity system {0}", type);
                //Force initialization of all systems
                var instance = _typeFactory.CreateInstance<IEntitySystem>(type);
                AddSystem(instance);
            }

            foreach (var system in _systems.Values)
            {
                system.Initialize();
            }
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
                _entityManager.EventBus.UnsubscribeEvents(_systems[type]);
                _systems.Remove(type);
            }
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            // System.Values is modified by RemoveSystem
            var values = _systems.Values.ToArray();
            foreach (var system in values)
            {
                RemoveSystem(system);
            }
        }

        /// <inheritdoc />
        public void Update(float frameTime)
        {
            foreach (var system in _systems.Values)
            {
                system.Update(frameTime);
            }
        }

        /// <inheritdoc />
        public void FrameUpdate(float frameTime)
        {
            foreach (var system in _systems.Values)
            {
                system.FrameUpdate(frameTime);
            }
        }
    }

    public class InvalidEntitySystemException : Exception { }
}
