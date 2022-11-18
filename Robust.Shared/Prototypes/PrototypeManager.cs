using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Asynchronous;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Robust.Shared.Prototypes
{
    [Virtual]
    public partial class PrototypeManager : IPrototypeManager
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] protected readonly IResourceManager Resources = default!;
        [Dependency] protected readonly ITaskManager TaskManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;

        private readonly Dictionary<string, Type> _kindNames = new();
        private readonly Dictionary<Type, int> _prototypePriorities = new();

        private ISawmill _sawmill = default!;

        private bool _initialized;
        private bool _hasEverBeenReloaded;

        #region IPrototypeManager members

        private readonly Dictionary<Type, KindData> _kinds = new();

        private readonly HashSet<string> _ignoredPrototypeTypes = new();

        public virtual void Initialize()
        {
            if (_initialized)
            {
                throw new InvalidOperationException($"{nameof(PrototypeManager)} has already been initialized.");
            }

            _sawmill = _logManager.GetSawmill("proto");

            _initialized = true;
            ReloadPrototypeKinds();
        }

        public IEnumerable<string> GetPrototypeKinds()
        {
            return _kindNames.Keys;
        }

        public IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            var data = _kinds[typeof(T)];

            foreach (var proto in data.Instances.Values)
            {
                yield return (T)proto;
            }
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(Type kind)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return _kinds[kind].Instances.Values;
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(string variant)
        {
            return EnumeratePrototypes(GetVariantType(variant));
        }

        public IEnumerable<T> EnumerateParents<T>(string kind, bool includeSelf = false)
            where T : class, IPrototype, IInheritingPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            if (!TryIndex<T>(kind, out var prototype))
                yield break;
            if (includeSelf) yield return prototype;
            if (prototype.Parents == null) yield break;

            var queue = new Queue<string>(prototype.Parents);
            while (queue.TryDequeue(out var prototypeId))
            {
                if (!TryIndex<T>(prototypeId, out var parent))
                    yield break;
                yield return parent;
                if (parent.Parents == null) continue;

                foreach (var parentId in parent.Parents)
                {
                    queue.Enqueue(parentId);
                }
            }
        }

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
            if (includeSelf) yield return prototype;
            var iPrototype = (IInheritingPrototype)prototype;
            if (iPrototype.Parents == null) yield break;

            var queue = new Queue<string>(iPrototype.Parents);
            while (queue.TryDequeue(out var prototypeId))
            {
                if (!TryIndex(kind, id, out var parent))
                    continue;
                yield return parent;
                iPrototype = (IInheritingPrototype)parent;
                if (iPrototype.Parents == null) continue;

                foreach (var parentId in iPrototype.Parents)
                {
                    queue.Enqueue(parentId);
                }
            }
        }

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
                throw new UnknownPrototypeException(id);
            }
        }

        public IPrototype Index(Type kind, string id)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return _kinds[kind].Instances[id];
        }

        public void Clear()
        {
            _kindNames.Clear();
            _kinds.Clear();
        }

        private int SortPrototypesByPriority(Type a, Type b)
        {
            return _prototypePriorities[b].CompareTo(_prototypePriorities[a]);
        }

        protected void ReloadPrototypes(IEnumerable<ResourcePath> filePaths)
        {
#if !FULL_RELEASE
            var changed = new Dictionary<Type, HashSet<string>>();
            foreach (var filePath in filePaths)
            {
                LoadFile(filePath.ToRootedPath(), true, changed);
            }

            ReloadPrototypes(changed);
#endif
        }

        public void ReloadPrototypes(Dictionary<Type, HashSet<string>> prototypes)
        {
#if !FULL_RELEASE
            var prototypeTypeOrder = prototypes.Keys.ToList();
            prototypeTypeOrder.Sort(SortPrototypesByPriority);

            var pushed = new Dictionary<Type, HashSet<string>>();

            foreach (var type in prototypeTypeOrder)
            {
                var typeData = _kinds[type];
                if (!type.IsAssignableTo(typeof(IInheritingPrototype)))
                {
                    foreach (var id in prototypes[type])
                    {
                        var prototype = (IPrototype)_serializationManager.Read(type, typeData.Results[id])!;
                        typeData.Instances[id] = prototype;
                    }

                    continue;
                }

                var tree = typeData.Inheritance!;
                var processQueue = new Queue<string>();
                foreach (var id in prototypes[type])
                {
                    processQueue.Enqueue(id);
                }

                while (processQueue.TryDequeue(out var id))
                {
                    var pushedSet = pushed.GetOrNew(type);

                    if (tree.TryGetParents(id, out var parents))
                    {
                        var nonPushedParent = false;
                        foreach (var parent in parents)
                        {
                            //our parent has been reloaded and has not been added to the pushedSet yet
                            if (prototypes[type].Contains(parent) && !pushedSet.Contains(parent))
                            {
                                //we re-queue ourselves at the end of the queue
                                processQueue.Enqueue(id);
                                nonPushedParent = true;
                                break;
                            }
                        }

                        if (nonPushedParent) continue;

                        foreach (var parent in parents)
                        {
                            PushInheritance(type, id, parent);
                        }
                    }

                    TryReadPrototype(type, id, typeData.Results[id]);

                    pushedSet.Add(id);
                }
            }

            //todo paul i hate it but i am not opening that can of worms in this refactor
            PrototypesReloaded?.Invoke(
                new PrototypesReloadedEventArgs(
                    prototypes
                        .ToDictionary(
                            g => g.Key,
                            g => new PrototypesReloadedEventArgs.PrototypeChangeSet(
                                g.Value.Where(x => _kinds[g.Key].Instances.ContainsKey(x))
                                    .ToDictionary(a => a, a => _kinds[g.Key].Instances[a])))));
#endif
        }

        /// <summary>
        /// Resolves the mappings stored in memory to actual prototypeinstances.
        /// </summary>
        public void ResolveResults()
        {
            var types = _kinds.Keys.ToList();
            types.Sort(SortPrototypesByPriority);
            foreach (var type in types)
            {
                var typeData = _kinds[type];
                if (typeData.Inheritance is { } tree)
                {
                    var processed = new HashSet<string>();
                    var workList = new Queue<string>(tree.RootNodes);

                    while (workList.TryDequeue(out var id))
                    {
                        processed.Add(id);
                        if (tree.TryGetParents(id, out var parents))
                        {
                            foreach (var parent in parents)
                            {
                                PushInheritance(type, id, parent);
                            }
                        }

                        if (tree.TryGetChildren(id, out var children))
                        {
                            foreach (var child in children)
                            {
                                var childParents = tree.GetParents(child)!;
                                if (childParents.All(p => processed.Contains(p)))
                                    workList.Enqueue(child);
                            }
                        }
                    }
                }

                foreach (var (id, mapping) in typeData.Results)
                {
                    TryReadPrototype(type, id, mapping);
                }
            }
        }

        private void TryReadPrototype(Type type, string id, MappingDataNode mapping)
        {
            if (mapping.TryGet<ValueDataNode>(AbstractDataFieldAttribute.Name, out var abstractNode) &&
                abstractNode.AsBool())
                return;
            try
            {
                _kinds[type].Instances[id] = (IPrototype)_serializationManager.Read(type, mapping)!;
            }
            catch (Exception e)
            {
                Logger.ErrorS("PROTO", $"Reading {type}({id}) threw the following exception: {e}");
            }
        }

        private void PushInheritance(Type type, string id, string parent)
        {
            var kindData = _kinds[type];

            kindData.Results[id] = _serializationManager.PushCompositionWithGenericNode(type,
                new[] { kindData.Results[parent] }, kindData.Results[id]);
        }

        #endregion IPrototypeManager members

        private void ReloadPrototypeKinds()
        {
            Clear();
            foreach (var type in _reflectionManager.GetAllChildren<IPrototype>())
            {
                RegisterKind(type);
            }
        }

        public bool HasIndex<T>(string id) where T : class, IPrototype
        {
            if (!_kinds.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id);
            }

            return index.Instances.ContainsKey(id);
        }

        public bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : class, IPrototype
        {
            var returned = TryIndex(typeof(T), id, out var proto);
            prototype = (proto ?? null) as T;
            return returned;
        }

        public bool TryIndex(Type kind, string id, [NotNullWhen(true)] out IPrototype? prototype)
        {
            if (!_kinds.TryGetValue(kind, out var index))
            {
                throw new UnknownPrototypeException(id);
            }

            return index.Instances.TryGetValue(id, out prototype);
        }

        public bool HasMapping<T>(string id)
        {
            if (!_kinds.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id);
            }

            return index.Results.ContainsKey(id);
        }

        public bool TryGetMapping(Type kind, string id, [NotNullWhen(true)] out MappingDataNode? mappings)
        {
            return _kinds[kind].Results.TryGetValue(id, out mappings);
        }

        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        public bool HasVariant(string variant) => HasKind(variant);

        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        public Type GetVariantType(string variant) => GetKindType(variant);

        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        public bool TryGetVariantType(string variant, [NotNullWhen(true)] out Type? prototype)
        {
            return TryGetKindType(variant, out prototype);
        }

        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        public bool TryGetVariantFrom(Type type, [NotNullWhen(true)] out string? variant)
        {
            return TryGetKindFrom(type, out variant);
        }

        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        public bool TryGetVariantFrom<T>([NotNullWhen(true)] out string? variant) where T : class, IPrototype
        {
            return TryGetKindFrom<T>(out variant);
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

            // If the type doesn't implement IPrototype, this fails.
            if (!(typeof(IPrototype).IsAssignableFrom(type)))
                return false;

            var attribute = (PrototypeAttribute?)Attribute.GetCustomAttribute(type, typeof(PrototypeAttribute));

            // If the prototype type doesn't have the attribute, this fails.
            if (attribute == null)
                return false;

            // If the variant isn't registered, this fails.
            if (!HasKind(attribute.Type))
                return false;

            kind = attribute.Type;
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

        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        public bool TryGetVariantFrom(IPrototype prototype, [NotNullWhen(true)] out string? variant)
        {
            return TryGetKindFrom(prototype, out variant);
        }

        public void RegisterIgnore(string name)
        {
            _ignoredPrototypeTypes.Add(name);
        }

        void IPrototypeManager.RegisterType(Type type) => RegisterKind(type);

        public void RegisterKind(Type kind)
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

            if (_kindNames.ContainsKey(attribute.Type))
            {
                throw new InvalidImplementationException(kind,
                    typeof(IPrototype),
                    $"Duplicate prototype type ID: {attribute.Type}. Current: {_kindNames[attribute.Type]}");
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
                        throw new InvalidImplementationException(kind,
                            typeof(IPrototype),
                            $"Found two {nameof(IdDataFieldAttribute)}");

                    foundIdAttribute = true;
                }

                if (hasParent)
                {
                    if (foundParentAttribute)
                        throw new InvalidImplementationException(kind,
                            typeof(IInheritingPrototype),
                            $"Found two {nameof(ParentDataFieldAttribute)}");

                    foundParentAttribute = true;
                }

                if (hasId && hasParent)
                    throw new InvalidImplementationException(kind,
                        typeof(IPrototype),
                        $"Prototype {kind} has the Id- & ParentDatafield on single member {info.Name}");

                if (info.HasAttribute<AbstractDataFieldAttribute>())
                {
                    if (foundAbstractAttribute)
                        throw new InvalidImplementationException(kind,
                            typeof(IInheritingPrototype),
                            $"Found two {nameof(AbstractDataFieldAttribute)}");

                    foundAbstractAttribute = true;
                }
            }

            if (!foundIdAttribute)
                throw new InvalidImplementationException(kind,
                    typeof(IPrototype),
                    $"Did not find any member annotated with the {nameof(IdDataFieldAttribute)}");

            if (kind.IsAssignableTo(typeof(IInheritingPrototype)) && (!foundParentAttribute || !foundAbstractAttribute))
                throw new InvalidImplementationException(kind,
                    typeof(IInheritingPrototype),
                    $"Did not find any member annotated with the {nameof(ParentDataFieldAttribute)} and/or {nameof(AbstractDataFieldAttribute)}");

            _kindNames[attribute.Type] = kind;
            _prototypePriorities[kind] = attribute.LoadPriority;

            var kindData = new KindData();
            _kinds[kind] = kindData;

            if (kind.IsAssignableTo(typeof(IInheritingPrototype)))
                kindData.Inheritance = new MultiRootInheritanceGraph<string>();
        }

        public event Action<PrototypesReloadedEventArgs>? PrototypesReloaded;

        private sealed class KindData
        {
            public readonly Dictionary<string, IPrototype> Instances = new();
            public readonly Dictionary<string, MappingDataNode> Results = new();

            // Only initialized if prototype is inheriting.
            public MultiRootInheritanceGraph<string>? Inheritance;
        }
    }
}
