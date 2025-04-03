using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Robust.Shared.Asynchronous;
using Robust.Shared.Collections;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Random;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Prototypes
{
    public abstract partial class PrototypeManager : IPrototypeManagerInternal
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] protected readonly IResourceManager Resources = default!;
        [Dependency] protected readonly ITaskManager TaskManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        [Dependency] private readonly ILocalizationManager _locMan = default!;
        [Dependency] private readonly IComponentFactory _factory = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        private readonly Dictionary<string, Dictionary<string, MappingDataNode>> _prototypeDataCache = new();
        private EntityDiffContext _context = new();

        private readonly Dictionary<string, Type> _kindNames = new();
        private readonly Dictionary<Type, int> _kindPriorities = new();

        protected ISawmill Sawmill = default!;

        private bool _initialized;
        private bool _hasEverBeenReloaded;

        #region IPrototypeManager members

        private FrozenDictionary<Type, KindData> _kinds = FrozenDictionary<Type, KindData>.Empty;

        private readonly HashSet<string> _ignoredPrototypeTypes = new();

        public virtual void Initialize()
        {
            if (_initialized)
                return;

            Sawmill = _logManager.GetSawmill("proto");

            _initialized = true;
            ReloadPrototypeKinds();
            PrototypesReloaded += OnReload;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetPrototypeKinds()
        {
            return _kindNames.Keys;
        }

        /// <inheritdoc />
        public int Count<T>() where T : class, IPrototype
        {
            return _kinds[typeof(T)].Instances.Count;
        }

        /// <inheritdoc />
        public IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype
        {
            return GetInstances<T>().Values;
        }

        /// <inheritdoc />
        public IEnumerable<IPrototype> EnumeratePrototypes(Type kind)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return _kinds[kind].Instances.Values;
        }

        /// <inheritdoc />
        public IEnumerable<IPrototype> EnumeratePrototypes(string variant)
        {
            return EnumeratePrototypes(GetKindType(variant));
        }

        /// <inheritdoc />
        public IEnumerable<T> EnumerateParents<T>(T proto, bool includeSelf = false)
            where T : class, IPrototype, IInheritingPrototype
        {
            return EnumerateParents<T>(proto.ID, includeSelf);
        }

        /// <inheritdoc />
        public IEnumerable<T> EnumerateParents<T>(string id, bool includeSelf = false)
            where T : class, IPrototype, IInheritingPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            if (!TryIndex<T>(id, out var prototype))
                yield break;

            if (includeSelf)
                yield return prototype;

            if (prototype.Parents == null)
                yield break;

            var queue = new Queue<string>(prototype.Parents);
            while (queue.TryDequeue(out var prototypeId))
            {
                if (!TryIndex<T>(prototypeId, out var parent))
                    continue; // Abstract parent?

                yield return parent;
                if (parent.Parents == null)
                    continue;

                foreach (var parentId in parent.Parents)
                {
                    queue.Enqueue(parentId);
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<IPrototype> EnumerateParents(Type kind, string id, bool includeSelf = false)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            if (!kind.IsAssignableTo(typeof(IInheritingPrototype)))
            {
                throw new InvalidOperationException("The provided prototype type is not an inheriting prototype");
            }

            if (!TryIndex(kind, id, out var prototype))
                yield break;

            if (includeSelf)
                yield return prototype;

            var iPrototype = (IInheritingPrototype)prototype;
            if (iPrototype.Parents == null)
                yield break;

            var queue = new Queue<string>(iPrototype.Parents);
            while (queue.TryDequeue(out var prototypeId))
            {
                if (!TryIndex(kind, prototypeId, out var parent))
                    continue; // Abstract parent?

                yield return parent;
                iPrototype = (IInheritingPrototype)parent;
                if (iPrototype.Parents == null)
                    continue;

                foreach (var parentId in iPrototype.Parents)
                {
                    queue.Enqueue(parentId);
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<(string id, T?)> EnumerateAllParents<T>(string id, bool includeSelf = false)
            where T : class, IPrototype, IInheritingPrototype
        {
            if (!_hasEverBeenReloaded)
                throw new InvalidOperationException("No prototypes have been loaded yet.");

            if (!_kinds.TryGetValue(typeof(T), out var kindData))
                throw new UnknownPrototypeException(id, typeof(T));

            if (!kindData.Results.ContainsKey(id))
                yield break;

            IPrototype? uncast;
            T? instance;

            if (includeSelf)
            {
                kindData.Instances.TryGetValue(id, out uncast);
                instance = uncast as T;
                yield return (id, instance);
            }

            if (!kindData.Inheritance!.TryGetParents(id, out var parents))
                yield break;

            var queue = new Queue<string>(parents);
            while (queue.TryDequeue(out var prototypeId))
            {
                if (!kindData.Results.ContainsKey(prototypeId))
                {
                    Sawmill.Error($"Encountered invalid prototype while enumerating parents. Kind: {typeof(T).Name}. Child: {id}. Invalid: {prototypeId}");
                    continue;
                }

                kindData.Instances.TryGetValue(prototypeId, out uncast);
                instance = uncast as T;
                yield return (prototypeId, instance);

                if (!kindData.Inheritance.TryGetParents(prototypeId, out parents))
                    continue;

                foreach (var parentId in parents)
                {
                    queue.Enqueue(parentId);
                }
            }
        }

        public IEnumerable<Type> EnumeratePrototypeKinds()
        {
            if (!_hasEverBeenReloaded)
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            return _kinds.Keys;
        }

        /// <inheritdoc />
        public T Index<T>(string id) where T : class, IPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            try
            {
                return (T)_kinds[typeof(T)].Instances[id];
            }
            catch (KeyNotFoundException)
            {
                throw new UnknownPrototypeException(id, typeof(T));
            }
        }

        /// <inheritdoc />
        public EntityPrototype Index(EntProtoId id)
        {
            return Index<EntityPrototype>(id.Id);
        }

        /// <inheritdoc />
        public T Index<T>(ProtoId<T> id) where T : class, IPrototype
        {
            return Index<T>(id.Id);
        }

        /// <inheritdoc />
        public IPrototype Index(Type kind, string id)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return _kinds[kind].Instances[id];
        }

        /// <inheritdoc />
        public void Clear()
        {
            _kindNames.Clear();
            _kinds = FrozenDictionary<Type, KindData>.Empty;
        }

        /// <inheritdoc />
        public void Reset()
        {
            var removed = _kinds.ToDictionary(
                x => x.Key,
                x => x.Value.Instances.Keys.ToHashSet());

            ReloadPrototypeKinds();
            Dictionary<Type, HashSet<string>> prototypes = new();
            LoadDefaultPrototypes(prototypes);

            foreach (var (kind, ids) in prototypes)
            {
                if (!removed.TryGetValue(kind, out var removedIds))
                    continue;

                removedIds.ExceptWith(ids);
                if (removedIds.Count == 0)
                    removed.Remove(kind);
            }

            ReloadPrototypes(prototypes, removed);
            _locMan.ReloadLocalizations();
        }

        /// <inheritdoc />
        public abstract void LoadDefaultPrototypes(Dictionary<Type, HashSet<string>>? changed = null);

        private int SortPrototypesByPriority(Type a, Type b)
        {
            return _kindPriorities[b].CompareTo(_kindPriorities[a]);
        }

        protected void ReloadPrototypes(IEnumerable<ResPath> filePaths)
        {
#if TOOLS
            var changed = new Dictionary<Type, HashSet<string>>();
            foreach (var filePath in filePaths)
            {
                LoadFile(filePath.ToRootedPath(), true, changed);
            }

            ReloadPrototypes(changed);
#endif
        }

        /// <inheritdoc />
        public void ReloadPrototypes(
            Dictionary<Type, HashSet<string>> modified,
            Dictionary<Type, HashSet<string>>? removed = null)
        {
            var prototypeTypeOrder = modified.Keys.ToList();
            prototypeTypeOrder.Sort(SortPrototypesByPriority);

            var byType = new Dictionary<Type, PrototypesReloadedEventArgs.PrototypeChangeSet>();
            var modifiedKinds = new HashSet<KindData>();
            var toProcess = new HashSet<string>();
            var processQueue = new Queue<string>();

            foreach (var kind in prototypeTypeOrder)
            {
                var modifiedInstances = new Dictionary<string, IPrototype>();
                var kindData = _kinds[kind];

                var tree = kindData.Inheritance;
                toProcess.Clear();
                processQueue.Clear();

                DebugTools.AssertEqual(kind.IsAssignableTo(typeof(IInheritingPrototype)), tree != null);
                DebugTools.Assert(tree != null || kindData.RawResults == kindData.Results);

                foreach (var id in modified[kind])
                {
                    AddToQueue(id);
                }

                void AddToQueue(string id)
                {
                    if (!toProcess.Add(id))
                        return;
                    processQueue.Enqueue(id);

                    if (tree == null)
                        return;

                    if (!tree.TryGetChildren(id, out var children))
                        return;

                    foreach (var child in children!)
                    {
                        AddToQueue(child);
                    }
                }

                while (processQueue.TryDequeue(out var id))
                {
                    DebugTools.Assert(toProcess.Contains(id));
                    if (tree != null)
                    {
                        if (tree.TryGetParents(id, out var parents))
                        {
                            DebugTools.Assert(parents.Length > 0);
                            var nonPushedParent = false;
                            foreach (var parent in parents)
                            {
                                if (!toProcess.Contains(parent))
                                    continue;

                                // our parent has been modified, but has not yet been processed.
                                // we re-queue ourselves at the end of the queue.
                                DebugTools.Assert(processQueue.Contains(parent));
                                processQueue.Enqueue(id);
                                nonPushedParent = true;
                                break;
                            }

                            if (nonPushedParent)
                                continue;

                            if (parents.Length == 1)
                            {
                                kindData.Results[id] = _serializationManager.PushCompositionWithGenericNode(
                                    kind,
                                    kindData.Results[parents[0]],
                                    kindData.RawResults[id]);
                            }
                            else
                            {
                                var parentMaps = new MappingDataNode[parents.Length];
                                for (var i = 0; i < parentMaps.Length; i++)
                                {
                                    parentMaps[i] = kindData.Results[parents[i]];
                                }

                                kindData.Results[id] = _serializationManager.PushCompositionWithGenericNode(
                                    kind,
                                    parentMaps,
                                    kindData.RawResults[id]);
                            }
                        }
                        else
                        {
                            kindData.Results[id] = kindData.RawResults[id];
                        }
                    }

                    toProcess.Remove(id);

                    var prototype = TryReadPrototype(kind, id, kindData.Results[id], SerializationHookContext.DontSkipHooks);
                    if (prototype == null)
                        continue;

                    kindData.UnfrozenInstances ??= kindData.Instances.ToDictionary();
                    kindData.UnfrozenInstances[id] = prototype;
                    modifiedInstances.Add(id, prototype);
                }

                if (modifiedInstances.Count == 0)
                    continue;

                byType.Add(kindData.Type, new(modifiedInstances));
                modifiedKinds.Add(kindData);
            }

            Freeze(modifiedKinds);

            if (modifiedKinds.Any(x => x.Type == typeof(EntityPrototype) || x.Type == typeof(EntityCategoryPrototype)))
                UpdateCategories();

            var modifiedTypes = new HashSet<Type>(byType.Keys);
            if (removed != null)
                modifiedTypes.UnionWith(removed.Keys);

            var ev = new PrototypesReloadedEventArgs(modifiedTypes, byType, removed);
            PrototypesReloaded?.Invoke(ev);
            _entMan.EventBus.RaiseEvent(EventSource.Local, ev);
        }

        private void Freeze(IEnumerable<KindData> kinds)
        {
            var st = RStopwatch.StartNew();
            foreach (var kind in kinds)
            {
                kind.Freeze();
            }

            // fun fact: Sawmill can be null in tests????
            Sawmill?.Verbose($"Freezing prototype instances took {st.Elapsed.TotalMilliseconds:f2}ms");
        }

        /// <summary>
        /// Resolves the mappings stored in memory to actual prototypeinstances.
        /// </summary>
        public void ResolveResults()
        {
            // Oh god I butchered this poor method in the name of my Ryzen CPU.

            // Run inheritance pushing concurrently to the rest of prototype loading.
            // Use some basic tasks and .Wait() to make sure it's done by the time we get to the prototypes in question.
            // The biggest prototypes by far in SS14 are entity prototypes.
            // Entity prototypes have priority -1 right now, so they have to be read last. This works out great!
            // We'll already be done pushing inheritance for them by the time we get to reading them.
            var inheritanceTasks = new Dictionary<Type, Task>();
            foreach (var (k, v) in _kinds)
            {
                if (v.Inheritance == null)
                    continue;

                var task = Task.Run(() => PushKindInheritance(k, v));
                inheritanceTasks.Add(k, task);
            }

            var priorities = _kinds.Keys
                .GroupBy(k => _kindPriorities[k])
                .OrderByDescending(k => k.Key);

            foreach (var group in priorities)
            {
                var kinds = group.Select(k => _kinds[k]).ToArray();
                InstantiateKinds(kinds, inheritanceTasks);
            }

            UpdateCategories();
        }

        private void InstantiateKinds(KindData[] kinds, Dictionary<Type, Task> inheritanceTasks)
        {
            // Wait for all inheritance pushing in this group to finish.
            // This isn't ideal, but since entity prototypes are the big ones in SS14 it's fine.
            foreach (var kind in kinds)
            {
                if (inheritanceTasks.TryGetValue(kind.Type, out var task))
                    task.Wait();
            }

            // Process all prototypes in this group in a single parallel operation.
            var results = kinds
                .SelectMany(data => data.Results,
                    (data, results) => (KindData: data, Id: results.Key, Mapping: results.Value, Instance: (IPrototype?)null))
                .ToArray();

            // Randomize to remove any patterns that could cause uneven load.
            _random.Shuffle(results.AsSpan());

            // Create channel that all AfterDeserialization hooks in this group will be sent into.
            var hooksChannelOptions = new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false,
                // Don't use an async job to unblock the read task.
                AllowSynchronousContinuations = true
            };

            var hooksChannel = Channel.CreateUnbounded<ISerializationHooks>(hooksChannelOptions);
            var instantiateTask = Task.Run(() => InstantiatePrototypes(kinds, results, hooksChannel));

            // On the game thread: process AfterDeserialization hooks from the channel.
            var channelReader = hooksChannel.Reader;
#pragma warning disable RA0004
            while (channelReader.WaitToReadAsync().AsTask().Result)
#pragma warning restore RA0004
            {
                while (channelReader.TryRead(out var hooks))
                {
                    hooks.AfterDeserialization();
                }
            }

            // Join task in case an exception was raised.
            instantiateTask.Wait();
        }

        private void InstantiatePrototypes(
            KindData[] kinds,
            (KindData KindData, string Id, MappingDataNode Mapping, IPrototype? Instance)[] results,
            Channel<ISerializationHooks> hooks)
        {
            var hookCtx = new SerializationHookContext(hooks.Writer, false);
            try
            {
                Parallel.For(0,
                    results.Length,
                    i =>
                    {
                        ref var item = ref results[i];
                        item.Instance = TryReadPrototype(item.KindData.Type, item.Id, item.Mapping, hookCtx);
                    });

                foreach (var item in results)
                {
                    if (item.Instance == null)
                        continue;
                    item.KindData.UnfrozenInstances ??= item.KindData.Instances.ToDictionary();
                    item.KindData.UnfrozenInstances[item.Id] = item.Instance;
                }

                Freeze(kinds.Where(data => data.UnfrozenInstances != null));
            }
            finally
            {
                // Mark the hooks channel as complete so the game thread unblocks.
                hooks.Writer.Complete();
            }
        }

        private IPrototype? TryReadPrototype(
            Type kind,
            string id,
            MappingDataNode mapping,
            SerializationHookContext hookCtx)
        {
            if (mapping.TryGet<ValueDataNode>(AbstractDataFieldAttribute.Name, out var abstractNode) &&
                abstractNode.AsBool())
                return null;

            try
            {
                return (IPrototype)_serializationManager.Read(kind, mapping, hookCtx)!;
            }
            catch (Exception e)
            {
                Sawmill.Error($"Reading {kind}({id}) threw the following exception: {e}");
                return null;
            }
        }

        private async Task PushKindInheritance(Type kind, KindData data)
        {
            if (data.Inheritance is not { } tree)
                return;

            // var sw = RStopwatch.StartNew();

            var results = data.RawResults.ToDictionary(
                k => k.Key,
                k => new InheritancePushDatum(k.Value, tree.GetParentsCount(k.Key)));

            using var countDown = new CountdownEvent(results.Count);

            foreach (var root in tree.RootNodes)
            {
                ThreadPool.QueueUserWorkItem(_ => { ProcessItem(root, results[root]); });
            }

            void ProcessItem(string id, InheritancePushDatum datum)
            {
                try
                {
                    if (tree.TryGetParents(id, out var parents))
                    {
                        if (parents.Length == 1)
                        {
                            datum.Result = _serializationManager.PushCompositionWithGenericNode(
                                kind,
                                results[parents[0]].Result,
                                datum.Result);
                        }
                        else
                        {
                            var parentNodes = new MappingDataNode[parents.Length];
                            for (var i = 0; i < parents.Length; i++)
                            {
                                parentNodes[i] = results[parents[i]].Result;
                            }

                            datum.Result = _serializationManager.PushCompositionWithGenericNode(
                                kind,
                                parentNodes,
                                datum.Result);
                        }
                    }

                    if (tree.TryGetChildren(id, out var children))
                    {
                        foreach (var child in children)
                        {
                            var childDatum = results[child];
                            var val = Interlocked.Decrement(ref childDatum.CountParentsRemaining);
                            if (val == 0)
                            {
                                ThreadPool.QueueUserWorkItem(_ => { ProcessItem(child, childDatum); });
                            }
                        }
                    }

                    // ReSharper disable once AccessToDisposedClosure
                    countDown.Signal();
                }
                catch (Exception e)
                {
                    Sawmill.Error($"Failed to push composition for {kind.Name} prototype with id: {id}. Exception: {e}");
                    throw;
                }
            }

            await WaitHandleHelpers.WaitOneAsync(countDown.WaitHandle);

            data.Results.Clear();
            foreach (var (k, v) in results)
            {
                data.Results[k] = v.Result;
            }

            // _sawmill.Debug($"Inheritance {kind}: {sw.Elapsed}");
        }

        private sealed class InheritancePushDatum
        {
            public MappingDataNode Result;
            public int CountParentsRemaining;

            public InheritancePushDatum(MappingDataNode result, int countParentsRemaining)
            {
                Result = result;
                CountParentsRemaining = countParentsRemaining;
            }
        }

        #endregion IPrototypeManager members

        /// <inheritdoc />
        public void ReloadPrototypeKinds()
        {
            Clear();
            var dict = new Dictionary<Type, KindData>();
            foreach (var type in _reflectionManager.GetAllChildren<IPrototype>())
            {
                RegisterKind(type, dict);
            }
            Freeze(dict);
        }

        /// <inheritdoc />
        public bool HasIndex<T>(string id) where T : class, IPrototype
        {
            if (!_kinds.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id, typeof(T));
            }

            return index.Instances.ContainsKey(id);
        }

        /// <inheritdoc />
        public bool HasIndex(EntProtoId id)
        {
            return HasIndex<EntityPrototype>(id.Id);
        }

        /// <inheritdoc />
        public bool HasIndex<T>(ProtoId<T> id) where T : class, IPrototype
        {
            return HasIndex<T>(id.Id);
        }

        /// <inheritdoc />
        public bool HasIndex(EntProtoId? id)
        {
            if (id == null)
                return false;

            return HasIndex(id.Value);
        }

        /// <inheritdoc />
        public bool HasIndex<T>(ProtoId<T>? id) where T : class, IPrototype
        {
            if (id == null)
                return false;

            return HasIndex(id.Value);
        }

        /// <inheritdoc />
        public bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : class, IPrototype
        {
            var returned = TryIndex(typeof(T), id, out var proto);
            prototype = (proto ?? null) as T;
            return returned;
        }

        /// <inheritdoc />
        public bool TryIndex(Type kind, string id, [NotNullWhen(true)] out IPrototype? prototype)
        {
            if (!_kinds.TryGetValue(kind, out var index))
            {
                throw new UnknownPrototypeException(id, kind);
            }

            return index.Instances.TryGetValue(id, out prototype);
        }

        /// <inheritdoc />
        public bool TryIndex(EntProtoId id, [NotNullWhen(true)] out EntityPrototype? prototype, bool logError = true)
        {
            if (TryIndex(id.Id, out prototype))
                return true;

            if (logError)
                Sawmill.Error($"Attempted to resolve invalid {nameof(EntProtoId)}: {id.Id}");
            return false;
        }

        /// <inheritdoc />
        public bool TryIndex<T>(ProtoId<T> id, [NotNullWhen(true)] out T? prototype, bool logError = true) where T : class, IPrototype
        {
            if (TryIndex(id.Id, out prototype))
                return true;

            if (logError)
                Sawmill.Error($"Attempted to resolve invalid ProtoId<{typeof(T).Name}>: {id.Id}");
            return false;
        }

        /// <inheritdoc />
        public bool TryIndex(EntProtoId? id, [NotNullWhen(true)] out EntityPrototype? prototype, bool logError = true)
        {
            if (id == null)
            {
                prototype = null;
                return false;
            }

            return TryIndex(id.Value, out prototype, logError);
        }

        /// <inheritdoc />
        public bool TryIndex<T>(ProtoId<T>? id, [NotNullWhen(true)] out T? prototype, bool logError = true) where T : class, IPrototype
        {
            if (id == null)
            {
                prototype = null;
                return false;
            }

            return TryIndex(id.Value, out prototype, logError);
        }

        /// <inheritdoc />
        public bool HasMapping<T>(string id)
        {
            if (!_kinds.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id, typeof(T));
            }

            return index.Results.ContainsKey(id);
        }

        /// <inheritdoc />
        public bool TryGetMapping(Type kind, string id, [NotNullWhen(true)] out MappingDataNode? mappings)
        {
            return _kinds[kind].Results.TryGetValue(id, out mappings);
        }

        public bool HasKind(string kind)
        {
            return _kindNames.ContainsKey(kind);
        }

        public Type GetKindType(string kind)
        {
            return _kindNames[kind];
        }

        public bool TryGetKindType(string kind, [NotNullWhen(true)] out Type? prototype)
        {
            return _kindNames.TryGetValue(kind, out prototype);
        }

        public bool TryGetKindFrom(Type type, [NotNullWhen(true)] out string? kind)
        {
            kind = null;
            if (!_kinds.TryGetValue(type, out var kindData))
                return false;

            kind = kindData.Name;
            return true;
        }

        public FrozenDictionary<string, T> GetInstances<T>() where T : IPrototype
        {
            if (TryGetInstances<T>(out var dict))
                return dict;

            throw new Exception($"Failed to fetch instances for kind {nameof(T)}");
        }

        public bool TryGetInstances<T>([NotNullWhen(true)] out FrozenDictionary<string, T>? instances)
            where T : IPrototype
        {
            if (!TryGetInstances(typeof(T), out var dict))
            {
                instances = null;
                return false;
            }

            DebugTools.Assert(dict is FrozenDictionary<string, T> || dict == null);
            instances = dict as FrozenDictionary<string, T>;

            // Prototypes with no loaded instances never get frozen.
            instances ??= FrozenDictionary<string, T>.Empty;
            return true;
        }

        private bool TryGetInstances(Type kind, [NotNullWhen(true)] out object? instances)
        {
            if (!_hasEverBeenReloaded)
                throw new InvalidOperationException("No prototypes have been loaded yet.");

            DebugTools.Assert(kind.IsAssignableTo(typeof(IPrototype)));
            if (!_kinds.TryGetValue(kind, out var kindData))
            {
                instances = null;
                return false;
            }

            instances = kindData.InstancesDirect;
            return true;
        }

        public bool TryGetKindFrom(IPrototype prototype, [NotNullWhen(true)] out string? kind)
        {
            return TryGetKindFrom(prototype.GetType(), out kind);
        }

        public bool TryGetKindFrom<T>([NotNullWhen(true)] out string? kind) where T : class, IPrototype
        {
            return TryGetKindFrom(typeof(T), out kind);
        }

        /// <inheritdoc />
        public void RegisterIgnore(string name)
        {
            _ignoredPrototypeTypes.Add(name);
        }

        static string CalculatePrototypeName(Type type)
        {
            const string prototype = "Prototype";
            if (!type.Name.EndsWith(prototype))
                throw new InvalidPrototypeNameException($"Prototype {type} must end with the word Prototype");

            var name = type.Name.AsSpan();
            return $"{char.ToLowerInvariant(name[0])}{name[1..^prototype.Length]}";
        }

        /// <inheritdoc />
        public void RegisterKind(params Type[] kinds)
        {
            var dict = _kinds.ToDictionary();
            foreach (var kind in kinds)
            {
                RegisterKind(kind, dict);
            }

            Freeze(dict);
        }

        private void Freeze(Dictionary<Type, KindData> dict)
        {
            var st = RStopwatch.StartNew();
            _kinds = dict.ToFrozenDictionary();

            // fun fact: Sawmill can be null in tests????
            Sawmill?.Verbose($"Freezing prototype kinds took {st.Elapsed.TotalMilliseconds:f2}ms");
        }

        private void RegisterKind(Type kind, Dictionary<Type, KindData> kinds)
        {
            if (!(typeof(IPrototype).IsAssignableFrom(kind)))
                throw new InvalidOperationException("Type must implement IPrototype.");

            var attribute = (PrototypeAttribute?)Attribute.GetCustomAttribute(kind, typeof(PrototypeAttribute));

            if (attribute == null)
            {
                throw new InvalidImplementationException(kind,
                    typeof(IPrototype),
                    "No " + nameof(PrototypeAttribute) + " to give it a type string.");
            }

            var name = attribute.Type ?? CalculatePrototypeName(kind);

            if (_kindNames.TryGetValue(name, out var existing))
            {
                throw new InvalidImplementationException(kind,
                    typeof(IPrototype),
                    $"Duplicate prototype type ID: {attribute.Type}. Current: {existing}");
            }

            var foundIdAttribute = false;
            var foundParentAttribute = false;
            var foundAbstractAttribute = false;
            foreach (var info in kind.GetAllPropertiesAndFields())
            {
                var hasId = info.HasAttribute<IdDataFieldAttribute>();
                var hasParent = info.HasAttribute<ParentDataFieldAttribute>();
                if (hasId)
                {
                    if (foundIdAttribute)
                    {
                        throw new InvalidImplementationException(kind,
                            typeof(IPrototype),
                            $"Found two {nameof(IdDataFieldAttribute)}");
                    }

                    foundIdAttribute = true;
                }

                if (hasParent)
                {
                    if (foundParentAttribute)
                    {
                        throw new InvalidImplementationException(kind,
                            typeof(IInheritingPrototype),
                            $"Found two {nameof(ParentDataFieldAttribute)}");
                    }

                    foundParentAttribute = true;
                }

                if (hasId && hasParent)
                {
                    throw new InvalidImplementationException(kind,
                        typeof(IPrototype),
                        $"Prototype {kind} has the Id- & ParentDatafield on single member {info.Name}");
                }

                if (info.HasAttribute<AbstractDataFieldAttribute>())
                {
                    if (foundAbstractAttribute)
                    {
                        throw new InvalidImplementationException(kind,
                            typeof(IInheritingPrototype),
                            $"Found two {nameof(AbstractDataFieldAttribute)}");
                    }

                    foundAbstractAttribute = true;
                }
            }

            if (!foundIdAttribute)
            {
                throw new InvalidImplementationException(kind,
                    typeof(IPrototype),
                    $"Did not find any member annotated with the {nameof(IdDataFieldAttribute)}");
            }

            if (kind.IsAssignableTo(typeof(IInheritingPrototype)) && (!foundParentAttribute || !foundAbstractAttribute))
            {
                throw new InvalidImplementationException(kind,
                    typeof(IInheritingPrototype),
                    $"Did not find any member annotated with the {nameof(ParentDataFieldAttribute)} and/or {nameof(AbstractDataFieldAttribute)}");
            }

            _kindNames[name] = kind;
            _kindPriorities[kind] = attribute.LoadPriority;

            var kindData = new KindData(kind, name);
            kinds[kind] = kindData;

            if (kind.IsAssignableTo(typeof(IInheritingPrototype)))
                kindData.Inheritance = new MultiRootInheritanceGraph<string>();
            else
                kindData.Results = kindData.RawResults;
        }

        /// <inheritdoc />
        public event Action<PrototypesReloadedEventArgs>? PrototypesReloaded;

        private sealed class KindData(Type kind, string name)
        {
            public Dictionary<string, IPrototype>? UnfrozenInstances;

            public FrozenDictionary<string, IPrototype> Instances = FrozenDictionary<string, IPrototype>.Empty;

            public Dictionary<string, MappingDataNode> Results = new();

            /// <summary>
            /// Variant of <see cref="Results"/> prior to inheritance pushing. If the kind does not have inheritance,
            /// then this is just the same dictionary.
            /// </summary>
            public readonly Dictionary<string, MappingDataNode> RawResults = new();

            public readonly Type Type = kind;
            public readonly string Name = name;

            // Only initialized if prototype is inheriting.
            public MultiRootInheritanceGraph<string>? Inheritance;

            /// <summary>
            /// Variant of <see cref="Instances"/> that has a direct mapping to the prototype kind. I.e., no IPrototype interface.
            /// </summary>
            public object InstancesDirect = default!;

            private MethodInfo _freezeDirectInfo = typeof(KindData)
                .GetMethod(nameof(FreezeDirect), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(kind);

            private void FreezeDirect<T>()
            {
                var dict = new Dictionary<string, T>();
                foreach (var (id, instance) in Instances)
                {
                    dict.Add(id, (T) instance);
                }
                InstancesDirect = dict.ToFrozenDictionary();
            }

            public void Freeze()
            {
                DebugTools.AssertNotNull(UnfrozenInstances);
                Instances = UnfrozenInstances?.ToFrozenDictionary() ?? FrozenDictionary<string, IPrototype>.Empty;
                UnfrozenInstances = null;
                _freezeDirectInfo.Invoke(this, null);
            }
        }

        private void OnReload(PrototypesReloadedEventArgs args)
        {
            if (args.ByType.TryGetValue(typeof(EntityPrototype), out var modified))
            {
                foreach (var id in modified.Modified.Keys)
                {
                    _prototypeDataCache.Remove(id);
                }
            }

            if (args.Removed == null || !args.Removed.TryGetValue(typeof(EntityPrototype), out var removed))
                return;

            foreach (var id in removed)
            {
                _prototypeDataCache.Remove(id);
            }
        }

        public IReadOnlyDictionary<string, MappingDataNode> GetPrototypeData(EntityPrototype prototype)
        {
            if (_prototypeDataCache.TryGetValue(prototype.ID, out var data))
                return data;

            _context.WritingReadingPrototypes = true;
            data = new();

            var xform = _factory.GetRegistration(typeof(TransformComponent)).Name;
            try
            {
                foreach (var (compType, comp) in prototype.Components)
                {
                    if (compType == xform)
                        continue;

                    var node = _serializationManager.WriteValueAs<MappingDataNode>(comp.Component.GetType(), comp.Component,
                        alwaysWrite: true, context: _context);
                    data.Add(compType, node);
                }
            }
            catch (Exception e)
            {
                Sawmill.Error($"Failed to convert prototype {prototype.ID} into yaml. Exception: {e.Message}");
            }

            _context.WritingReadingPrototypes = false;
            _prototypeDataCache[prototype.ID] = data;
            return data;
        }

        public bool TryGetRandom<T>(IRobustRandom random, [NotNullWhen(true)] out IPrototype? prototype) where T : class, IPrototype
        {
            var count = Count<T>();

            if (count == 0)
            {
                prototype = null;
                return false;
            }

            var index = 0;

            var picked = random.Next(count);

            foreach (var proto in EnumeratePrototypes<T>())
            {
                if (index == picked)
                {
                    prototype = proto;
                    return true;
                }

                index++;
            }

            throw new ArgumentOutOfRangeException($"Unable to pick valid prototype for {typeof(T)}?");
        }
    }

    public sealed class InvalidPrototypeNameException : Exception
    {
        public InvalidPrototypeNameException(string message) : base(message)
        {
        }
    }
}
