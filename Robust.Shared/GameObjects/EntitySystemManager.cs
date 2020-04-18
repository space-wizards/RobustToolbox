using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Systems;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

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
        [ViewVariables]
        private IReadOnlyCollection<IEntitySystem> AllSystems => _systems.Values;

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
                // Force IoC inject of all systems
                var instance = _typeFactory.CreateInstance<IEntitySystem>(type);

                _systems.Add(type, instance);
            }

            foreach (var system in _systems.Values)
            {
                system.Initialize();
            }
        }

        /// <inheritdoc />
        public void Shutdown()
        {
            // System.Values is modified by RemoveSystem
            foreach (var system in _systems.Values)
            {
                system.Shutdown();
                _entityManager.EventBus.UnsubscribeEvents(system);
            }

            _systems.Clear();
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
