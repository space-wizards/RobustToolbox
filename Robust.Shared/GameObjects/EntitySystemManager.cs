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
        [Dependency] private readonly IEntityManager _entityManager = default!;

#if EXCEPTION_TOLERANCE
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif

        private DependencyCollection _systemDependencyCollection = default!;
        private List<Type> _systemTypes = new();

        private static readonly Histogram _tickUsageHistogram = Metrics.CreateHistogram("robust_entity_systems_update_usage",
            "Amount of time spent processing each entity system", new HistogramConfiguration
            {
                LabelNames = new[] {"system"},
                Buckets = Histogram.ExponentialBuckets(0.000_001, 1.5, 25)
            });

        [ViewVariables]
        private readonly List<Type> _extraLoadedTypes = new();

        private readonly Stopwatch _stopwatch = new();

        private bool _initialized;

        [ViewVariables] private UpdateReg[] _updateOrder = Array.Empty<UpdateReg>();
        [ViewVariables] private IEntitySystem[] _frameUpdateOrder = Array.Empty<IEntitySystem>();

        public bool MetricsEnabled { get; set; }

        /// <inheritdoc />
        public event EventHandler<SystemChangedArgs>? SystemLoaded;

        /// <inheritdoc />
        public event EventHandler<SystemChangedArgs>? SystemUnloaded;

        /// <exception cref="InvalidEntitySystemException">Thrown if the provided type is not registered.</exception>
        public T GetEntitySystem<T>()
            where T : IEntitySystem
        {
            return _systemDependencyCollection.Resolve<T>();
        }

        /// <inheritdoc />
        public bool TryGetEntitySystem<T>([NotNullWhen(true)] out T? entitySystem)
            where T : IEntitySystem
        {
            return _systemDependencyCollection.TryResolveType<T>(out entitySystem);
        }

        /// <inheritdoc />
        public void Initialize()
        {
            var excludedTypes = new HashSet<Type>();

            _systemDependencyCollection = new(IoCManager.Instance!);
            var subTypes = new Dictionary<Type, Type>();
            _systemTypes.Clear();
            foreach (var type in _reflectionManager.GetAllChildren<IEntitySystem>().Concat(_extraLoadedTypes))
            {
                Logger.DebugS("go.sys", "Initializing entity system {0}", type);

                _systemDependencyCollection.Register(type);
                _systemTypes.Add(type);

                excludedTypes.Add(type);
                if (subTypes.ContainsKey(type))
                {
                    subTypes.Remove(type);
                }

                // also register systems under their supertypes, so they can be retrieved by their supertype.
                // We don't do this if there are multiple subtype systems of that supertype though, otherwise
                // it wouldn't be clear which instance to return when asking for the supertype
                foreach (var baseType in GetBaseTypes(type))
                {
                    // already known that there are multiple subtype systems of this type,
                    // so don't register under the supertype because it would be unclear
                    // which instance to return if we retrieved it by the supertype
                    if (excludedTypes.Contains(baseType)) continue;

                    if (subTypes.ContainsKey(baseType))
                    {
                        subTypes.Remove(baseType);
                        excludedTypes.Add(baseType);
                    }
                    else
                    {
                        subTypes.Add(baseType, type);
                    }
                }
            }

            foreach (var (baseType, type) in subTypes)
            {
                _systemDependencyCollection.Register(baseType, type, overwrite: true);
                _systemTypes.Remove(baseType);
            }

            _systemDependencyCollection.BuildGraph();

            foreach (var systemType in _systemTypes)
            {
                var system = (IEntitySystem)_systemDependencyCollection.ResolveType(systemType);
                system.Initialize();
                SystemLoaded?.Invoke(this, new SystemChangedArgs(system));
            }

            // Create update order for entity systems.
            var (fUpdate, update) = CalculateUpdateOrder(_systemTypes, subTypes, _systemDependencyCollection);

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
                List<Type> systemTypes,
                Dictionary<Type, Type> subTypes,
                DependencyCollection dependencyCollection)
        {
            var allNodes = new List<TopologicalSort.GraphNode<IEntitySystem>>();
            var typeToNode = new Dictionary<Type, TopologicalSort.GraphNode<IEntitySystem>>();

            foreach (var systemType in systemTypes)
            {
                var node = new TopologicalSort.GraphNode<IEntitySystem>((IEntitySystem) dependencyCollection.ResolveType(systemType));

                typeToNode.Add(systemType, node);
                allNodes.Add(node);
            }

            foreach (var (type, system) in subTypes)
            {
                var node = typeToNode[system];
                typeToNode[type] = node;
            }

            foreach (var node in typeToNode.Values)
            {
                foreach (var after in node.Value.UpdatesAfter)
                {
                    var system = typeToNode[after];

                    system.Dependant.Add(node);
                }

                foreach (var before in node.Value.UpdatesBefore)
                {
                    var system = typeToNode[before];

                    node.Dependant.Add(system);
                }
            }

            var order = TopologicalSort.Sort(allNodes).ToArray();
            var frameUpdate = order.Where(p => NeedsFrameUpdate(p.GetType()));
            var update = order.Where(p => NeedsUpdate(p.GetType()));

            return (frameUpdate, update);
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
            foreach (var systemType in _systemTypes)
            {
                if(_systemDependencyCollection == null) continue;
                var system = (IEntitySystem)_systemDependencyCollection.ResolveType(systemType);
                SystemUnloaded?.Invoke(this, new SystemChangedArgs(system));
                system.Shutdown();
                _entityManager.EventBus.UnsubscribeEvents(system);
            }

            Clear();
        }

        public void Clear()
        {
            _systemTypes.Clear();
            _updateOrder = Array.Empty<UpdateReg>();
            _frameUpdateOrder = Array.Empty<IEntitySystem>();
            _initialized = false;
            _systemDependencyCollection?.Clear();
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
}
