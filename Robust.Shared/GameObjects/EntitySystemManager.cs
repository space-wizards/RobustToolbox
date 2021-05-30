using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Prometheus;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
#if EXCEPTION_TOLERANCE
using Robust.Shared.Exceptions;
#endif


namespace Robust.Shared.GameObjects
{
    public class EntitySystemManager : IEntitySystemManager
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IDynamicTypeFactoryInternal _typeFactory = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

#if EXCEPTION_TOLERANCE
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif

        private static readonly Histogram _tickUsageHistogram = Metrics.CreateHistogram("robust_entity_systems_update_usage",
            "Amount of time spent processing each entity system", new HistogramConfiguration
            {
                LabelNames = new[] {"system"},
                Buckets = Histogram.ExponentialBuckets(0.000_001, 1.5, 25)
            });

        [ViewVariables]
        private readonly List<Type> _extraLoadedTypes = new();

        private readonly Stopwatch _stopwatch = new();

        /// <summary>
        /// Maps system types to instances.
        /// </summary>
        [ViewVariables]
        private readonly Dictionary<Type, IEntitySystem> _systems = new();
        /// <summary>
        /// Maps system supertypes to instances.
        /// </summary>
        [ViewVariables]
        private readonly Dictionary<Type, IEntitySystem> _supertypeSystems = new();

        private bool _initialized;

        [ViewVariables] private UpdateReg[] _updateOrder = Array.Empty<UpdateReg>();
        [ViewVariables] private IEntitySystem[] _frameUpdateOrder = Array.Empty<IEntitySystem>();

        [ViewVariables] public IReadOnlyCollection<IEntitySystem> AllSystems => _systems.Values;

        public bool MetricsEnabled { get; set; }

        /// <inheritdoc />
        public event EventHandler<SystemChangedArgs>? SystemLoaded;

        /// <inheritdoc />
        public event EventHandler<SystemChangedArgs>? SystemUnloaded;

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
        public bool TryGetEntitySystem<T>([NotNullWhen(true)] out T? entitySystem)
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
            HashSet<Type> excludedTypes = new();

            foreach (var type in _reflectionManager.GetAllChildren<IEntitySystem>().Concat(_extraLoadedTypes))
            {
                Logger.DebugS("go.sys", "Initializing entity system {0}", type);
                // Force IoC inject of all systems
                var instance = _typeFactory.CreateInstanceUnchecked<IEntitySystem>(type);

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
                SystemLoaded?.Invoke(this, new SystemChangedArgs(system));
            }

            // Create update order for entity systems.
            var (fUpdate, update) = CalculateUpdateOrder(_systems.Values, _supertypeSystems);

            _frameUpdateOrder = fUpdate.ToArray();
            _updateOrder = update
                .Select(s => new UpdateReg
                {
                    System = s,
                    Monitor = _tickUsageHistogram.WithLabels(s.GetType().Name)
                })
                .ToArray();

            _initialized = true;
        }

        private static (IEnumerable<IEntitySystem> frameUpd, IEnumerable<IEntitySystem> upd)
            CalculateUpdateOrder(
                Dictionary<Type, IEntitySystem>.ValueCollection systems,
                Dictionary<Type, IEntitySystem> supertypeSystems)
        {
            var allNodes = new List<GraphNode<IEntitySystem>>();
            var typeToNode = new Dictionary<Type, GraphNode<IEntitySystem>>();

            foreach (var system in systems)
            {
                var node = new GraphNode<IEntitySystem>(system);

                allNodes.Add(node);
                typeToNode.Add(system.GetType(), node);
            }

            foreach (var (type, system) in supertypeSystems)
            {
                var node = typeToNode[system.GetType()];
                typeToNode[type] = node;
            }

            foreach (var node in allNodes)
            {
                foreach (var after in node.System.UpdatesAfter)
                {
                    var system = typeToNode[after];

                    node.DependsOn.Add(system);
                }

                foreach (var before in node.System.UpdatesBefore)
                {
                    var system = typeToNode[before];

                    system.DependsOn.Add(node);
                }
            }

            var order = TopologicalSort(allNodes).Select(p => p.System).ToArray();
            var frameUpdate = order.Where(p => NeedsFrameUpdate(p.GetType()));
            var update = order.Where(p => NeedsUpdate(p.GetType()));

            return (frameUpdate, update);
        }

        internal static IEnumerable<GraphNode<T>> TopologicalSort<T>(IEnumerable<GraphNode<T>> nodes)
        {
            var elems = nodes.ToDictionary(node => node,
                node => new HashSet<GraphNode<T>>(node.DependsOn));
            while (elems.Count > 0)
            {
                var elem =
                    elems.FirstOrDefault(x => x.Value.Count == 0);
                if (elem.Key == null)
                {
                    throw new InvalidOperationException(
                        "Found circular dependency when resolving entity system update dependency graph");
                }
                elems.Remove(elem.Key);
                foreach (var selem in elems)
                {
                    selem.Value.Remove(elem.Key);
                }
                yield return elem.Key;
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
                SystemUnloaded?.Invoke(this, new SystemChangedArgs(system));
                system.Shutdown();
                _entityManager.EventBus.UnsubscribeEvents(system);
            }

            _systems.Clear();
            _updateOrder = Array.Empty<UpdateReg>();
            _frameUpdateOrder = Array.Empty<IEntitySystem>();
            _supertypeSystems.Clear();
            _initialized = false;
        }

        /// <inheritdoc />
        public void TickUpdate(float frameTime)
        {
            foreach (var updReg in _updateOrder)
            {
                if (MetricsEnabled)
                {
                    _stopwatch.Restart();
                }
#if EXCEPTION_TOLERANCE
                try
                {
#endif
                    updReg.System.Update(frameTime);
#if EXCEPTION_TOLERANCE
                }
                catch (Exception e)
                {
                    _runtimeLog.LogException(e, "entsys");
                }
#endif

                if (MetricsEnabled)
                {
                    updReg.Monitor.Observe(_stopwatch.Elapsed.TotalSeconds);
                }
            }
        }

        /// <inheritdoc />
        public void FrameUpdate(float frameTime)
        {
            foreach (var system in _frameUpdateOrder)
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
                    _runtimeLog.LogException(e, "entsys");
                }
#endif
            }
        }

        public void LoadExtraSystemType<T>() where T : IEntitySystem, new()
        {
            if (_initialized)
            {
                throw new InvalidOperationException(
                    "Cannot use LoadExtraSystemType when the entity system manager is initialized.");
            }

            _extraLoadedTypes.Add(typeof(T));
        }

        private static bool NeedsUpdate(Type type)
        {
            if (!typeof(EntitySystem).IsAssignableFrom(type))
            {
                return true;
            }

            var mUpdate = type.GetMethod(nameof(EntitySystem.Update), new[] {typeof(float)});

            DebugTools.AssertNotNull(mUpdate);

            return mUpdate!.DeclaringType != typeof(EntitySystem);
        }

        private static bool NeedsFrameUpdate(Type type)
        {
            if (!typeof(EntitySystem).IsAssignableFrom(type))
            {
                return true;
            }

            var mFrameUpdate = type.GetMethod(nameof(EntitySystem.FrameUpdate), new[] {typeof(float)});

            DebugTools.AssertNotNull(mFrameUpdate);

            return mFrameUpdate!.DeclaringType != typeof(EntitySystem);
        }

        [DebuggerDisplay("GraphNode: {" + nameof(System) + "}")]
        internal sealed class GraphNode<T>
        {
            public readonly T System;
            public readonly List<GraphNode<T>> DependsOn = new();

            public GraphNode(T system)
            {
                System = system;
            }
        }

        private struct UpdateReg
        {
            [ViewVariables] public IEntitySystem System;
            [ViewVariables] public Histogram.Child Monitor;

            public override string? ToString()
            {
                return System.ToString();
            }
        }
    }

    public class SystemChangedArgs : EventArgs
    {
        public IEntitySystem System { get; }

        public SystemChangedArgs(IEntitySystem system)
        {
            System = system;
        }
    }

    public class InvalidEntitySystemException : Exception { }
}
