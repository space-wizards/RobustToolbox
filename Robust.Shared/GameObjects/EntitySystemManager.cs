using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IDynamicTypeFactory _typeFactory = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
#pragma warning restore 649

        /// <summary>
        /// Maps system types to instances.
        /// </summary>
        private readonly Dictionary<Type, IEntitySystem> _systems = new Dictionary<Type, IEntitySystem>();
        /// <summary>
        /// Maps system supertypes to instances.
        /// </summary>
        private readonly Dictionary<Type, IEntitySystem> _supertypeSystems = new Dictionary<Type, IEntitySystem>();
        [ViewVariables]
        private IReadOnlyCollection<IEntitySystem> AllSystems => _systems.Values;

        /// <exception cref="InvalidEntitySystemException">Thrown if the provided type is not registered.</exception>
        public T GetEntitySystem<T>()
            where T : IEntitySystem
        {
            var type = typeof(T);
            // check using exact match first, then check using the supertype
            if (!_systems.ContainsKey(type))
            {
                if (!_supertypeSystems.ContainsKey(type))
                {
                    throw new InvalidEntitySystemException();
                }
                else
                {
                    return (T) _supertypeSystems[type];
                }
            }

            return (T)_systems[type];
        }

        /// <inheritdoc />
        public bool TryGetEntitySystem<T>([MaybeNullWhen(false)] out T entitySystem)
            where T : IEntitySystem
        {
            if (_systems.TryGetValue(typeof(T), out var system))
            {
                entitySystem = (T) system;
                return true;
            }

            if (_supertypeSystems.TryGetValue(typeof(T), out var systemFromSupertype))
            {
                entitySystem = (T) systemFromSupertype;
                return true;
            }

            entitySystem = default;
            return false;
        }

        /// <inheritdoc />
        public void Initialize()
        {
            HashSet<Type> excludedTypes = new HashSet<Type>();

            foreach (var type in _reflectionManager.GetAllChildren<IEntitySystem>())
            {
                Logger.DebugS("go.sys", "Initializing entity system {0}", type);
                // Force IoC inject of all systems
                var instance = _typeFactory.CreateInstance<IEntitySystem>(type);

                _systems.Add(type, instance);

                // also register systems under their supertypes, so they can be retrieved by their supertype.
                // We don't do this if there are multiple subtype systems of that supertype though, otherwise
                // it wouldn't be clear which instance to return when asking for the supertype
                foreach (var baseType in GetBaseTypes(type))
                {
                    // already known that there are multiple subtype systems of this type,
                    // so don't register under the supertype because it would be unclear
                    // which instance to return if we retrieved it by the supertype
                    if (excludedTypes.Contains(baseType)) continue;
                    if (_supertypeSystems.ContainsKey(baseType))
                    {
                        _supertypeSystems.Remove(baseType);
                        excludedTypes.Add(baseType);
                    }
                    else
                    {
                        _supertypeSystems.Add(baseType, instance);
                    }
                }

            }

            foreach (var system in _systems.Values)
            {
                system.Initialize();
            }
        }

        private static IEnumerable<Type> GetBaseTypes(Type type) {
            if(type.BaseType == null) return type.GetInterfaces();

            return Enumerable.Repeat(type.BaseType, 1)
                .Concat(type.GetInterfaces())
                .Concat(type.GetInterfaces().SelectMany<Type, Type>(GetBaseTypes))
                .Concat(GetBaseTypes(type.BaseType));
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
            _supertypeSystems.Clear();
        }

        /// <inheritdoc />
        public void Update(float frameTime)
        {
            foreach (var system in _systems.Values)
            {
#if EXCEPTION_TOLERANCE
                try
                {
#endif
                system.Update(frameTime);
#if EXCEPTION_TOLERANCE
                }
                catch (Exception e)
                {
                    Logger.ErrorS("entsys", e.ToString());
                }
#endif
            }
        }

        /// <inheritdoc />
        public void FrameUpdate(float frameTime)
        {
            foreach (var system in _systems.Values)
            {
#if EXCEPTION_TOLERANCE
                try
                {
#endif
                    system.FrameUpdate(frameTime);
#if EXCEPTION_TOLERANCE
                }
                catch (Exception e)
                {
                    Logger.ErrorS("entsys", e.ToString());
                }
#endif
            }
        }
    }

    public class InvalidEntitySystemException : Exception { }
}
