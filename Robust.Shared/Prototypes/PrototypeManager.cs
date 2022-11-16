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

        private readonly Dictionary<string, Type> _prototypeTypes = new();
        private readonly Dictionary<Type, int> _prototypePriorities = new();

        private bool _initialized;
        private bool _hasEverBeenReloaded;

        #region IPrototypeManager members

        private readonly Dictionary<Type, Dictionary<string, IPrototype>> _prototypes = new();
        private readonly Dictionary<Type, Dictionary<string, MappingDataNode>> _prototypeResults = new();
        private readonly Dictionary<Type, MultiRootInheritanceGraph<string>> _inheritanceTrees = new();

        private readonly HashSet<string> _ignoredPrototypeTypes = new();

        public virtual void Initialize()
        {
            if (_initialized)
            {
                throw new InvalidOperationException($"{nameof(PrototypeManager)} has already been initialized.");
            }

            _initialized = true;
            ReloadPrototypeTypes();
        }

        public IEnumerable<string> GetPrototypeKinds()
        {
            return _prototypeTypes.Keys;
        }

        public IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            var protos = _prototypes[typeof(T)];

            foreach (var (_, proto) in protos)
            {
                yield return (T) proto;
            }
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(Type type)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return _prototypes[type].Values;
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(string variant)
        {
            return EnumeratePrototypes(GetVariantType(variant));
        }

        public IEnumerable<T> EnumerateParents<T>(string id, bool includeSelf = false)  where T : class, IPrototype, IInheritingPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            if(!TryIndex<T>(id, out var prototype))
                yield break;
            if (includeSelf) yield return prototype;
            if (prototype.Parents == null) yield break;

            var queue = new Queue<string>(prototype.Parents);
            while (queue.TryDequeue(out var prototypeId))
            {
                if(!TryIndex<T>(prototypeId, out var parent))
                    yield break;
                yield return parent;
                if (parent.Parents == null) continue;

                foreach (var parentId in parent.Parents)
                {
                    queue.Enqueue(parentId);
                }
            }
        }

        public IEnumerable<IPrototype> EnumerateParents(Type type, string id, bool includeSelf = false)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            if (!type.IsAssignableTo(typeof(IInheritingPrototype)))
            {
                throw new InvalidOperationException("The provided prototype type is not an inheriting prototype");
            }

            if(!TryIndex(type, id, out var prototype))
                yield break;
            if (includeSelf) yield return prototype;
            var iPrototype = (IInheritingPrototype)prototype;
            if (iPrototype.Parents == null) yield break;

            var queue = new Queue<string>(iPrototype.Parents);
            while (queue.TryDequeue(out var prototypeId))
            {
                if (!TryIndex(type, id, out var parent))
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
                return (T) _prototypes[typeof(T)][id];
            }
            catch (KeyNotFoundException)
            {
                throw new UnknownPrototypeException(id);
            }
        }

        public IPrototype Index(Type type, string id)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return _prototypes[type][id];
        }

        public void Clear()
        {
            _prototypeTypes.Clear();
            _prototypes.Clear();
            _prototypeResults.Clear();
            _inheritanceTrees.Clear();
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
                if (!type.IsAssignableTo(typeof(IInheritingPrototype)))
                {
                    foreach (var id in prototypes[type])
                    {
                        _prototypes[type][id] = (IPrototype) _serializationManager.Read(type, _prototypeResults[type][id])!;
                    }
                    continue;
                }

                var tree = _inheritanceTrees[type];
                var processQueue = new Queue<string>();
                foreach (var id in prototypes[type])
                {
                    processQueue.Enqueue(id);
                }

                while(processQueue.TryDequeue(out var id))
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
                        if(nonPushedParent) continue;

                        foreach (var parent in parents)
                        {
                            PushInheritance(type, id, parent);
                        }
                    }

                    TryReadPrototype(type, id, _prototypeResults[type][id]);

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
                                g.Value.Where(x => _prototypes[g.Key].ContainsKey(x)).ToDictionary(a => a, a => _prototypes[g.Key][a])))));
#endif
        }

        /// <summary>
        /// Resolves the mappings stored in memory to actual prototypeinstances.
        /// </summary>
        public void ResolveResults()
        {
            var types = _prototypeResults.Keys.ToList();
            types.Sort(SortPrototypesByPriority);
            foreach (var type in types)
            {
                if(_inheritanceTrees.TryGetValue(type, out var tree))
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
                                if(childParents.All(p => processed.Contains(p)))
                                    workList.Enqueue(child);
                            }
                        }
                    }
                }

                foreach (var (id, mapping) in _prototypeResults[type])
                {
                    TryReadPrototype(type, id, mapping);
                }
            }
        }

        private void TryReadPrototype(Type type, string id, MappingDataNode mapping)
        {
            if(mapping.TryGet<ValueDataNode>(AbstractDataFieldAttribute.Name, out var abstractNode) && abstractNode.AsBool())
                return;
            try
            {
                _prototypes[type][id] = (IPrototype) _serializationManager.Read(type, mapping)!;
            }
            catch (Exception e)
            {
                Logger.ErrorS("PROTO", $"Reading {type}({id}) threw the following exception: {e}");
            }
        }

        private void PushInheritance(Type type, string id, string parent)
        {
            _prototypeResults[type][id] = _serializationManager.PushCompositionWithGenericNode(type,
                new[] { _prototypeResults[type][parent] }, _prototypeResults[type][id]);
        }

        #endregion IPrototypeManager members

        private void ReloadPrototypeTypes()
        {
            Clear();
            foreach (var type in _reflectionManager.GetAllChildren<IPrototype>())
            {
                RegisterType(type);
            }
        }

        public bool HasIndex<T>(string id) where T : class, IPrototype
        {
            if (!_prototypes.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id);
            }

            return index.ContainsKey(id);
        }

        public bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : class, IPrototype
        {
            var returned = TryIndex(typeof(T), id, out var proto);
            prototype = (proto ?? null) as T;
            return returned;
        }

        public bool TryIndex(Type type, string id, [NotNullWhen(true)] out IPrototype? prototype)
        {
            if (!_prototypes.TryGetValue(type, out var index))
            {
                throw new UnknownPrototypeException(id);
            }

            return index.TryGetValue(id, out prototype);
        }

        public bool HasMapping<T>(string id)
        {
            if (!_prototypeResults.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id);
            }

            return index.ContainsKey(id);
        }

        public bool TryGetMapping(Type type, string id, [NotNullWhen(true)] out MappingDataNode? mappings)
        {
            return _prototypeResults[type].TryGetValue(id, out mappings);
        }

        /// <inheritdoc />
        public bool HasVariant(string variant)
        {
            return _prototypeTypes.ContainsKey(variant);
        }

        /// <inheritdoc />
        public Type GetVariantType(string variant)
        {
            return _prototypeTypes[variant];
        }

        /// <inheritdoc />
        public bool TryGetVariantType(string variant, [NotNullWhen(true)] out Type? prototype)
        {
            return _prototypeTypes.TryGetValue(variant, out prototype);
        }

        /// <inheritdoc />
        public bool TryGetVariantFrom(Type type, [NotNullWhen(true)] out string? variant)
        {
            variant = null;

            // If the type doesn't implement IPrototype, this fails.
            if (!(typeof(IPrototype).IsAssignableFrom(type)))
                return false;

            var attribute = (PrototypeAttribute?) Attribute.GetCustomAttribute(type, typeof(PrototypeAttribute));

            // If the prototype type doesn't have the attribute, this fails.
            if (attribute == null)
                return false;

            // If the variant isn't registered, this fails.
            if (!HasVariant(attribute.Type))
                return false;

            variant = attribute.Type;
            return true;
        }

        /// <inheritdoc />
        public bool TryGetVariantFrom<T>([NotNullWhen(true)] out string? variant) where T : class, IPrototype
        {
            return TryGetVariantFrom(typeof(T), out variant);
        }

        /// <inheritdoc />
        public bool TryGetVariantFrom(IPrototype prototype, [NotNullWhen(true)] out string? variant)
        {
            return TryGetVariantFrom(prototype.GetType(), out variant);
        }

        public void RegisterIgnore(string name)
        {
            _ignoredPrototypeTypes.Add(name);
        }

        /// <inheritdoc />
        public void RegisterType(Type type)
        {
            if (!(typeof(IPrototype).IsAssignableFrom(type)))
                throw new InvalidOperationException("Type must implement IPrototype.");

            var attribute = (PrototypeAttribute?) Attribute.GetCustomAttribute(type, typeof(PrototypeAttribute));

            if (attribute == null)
            {
                throw new InvalidImplementationException(type,
                    typeof(IPrototype),
                    "No " + nameof(PrototypeAttribute) + " to give it a type string.");
            }

            if (_prototypeTypes.ContainsKey(attribute.Type))
            {
                throw new InvalidImplementationException(type,
                    typeof(IPrototype),
                    $"Duplicate prototype type ID: {attribute.Type}. Current: {_prototypeTypes[attribute.Type]}");
            }

            var foundIdAttribute = false;
            var foundParentAttribute = false;
            var foundAbstractAttribute = false;
            foreach (var info in type.GetAllPropertiesAndFields())
            {
                var hasId = info.HasAttribute<IdDataFieldAttribute>();
                var hasParent = info.HasAttribute<ParentDataFieldAttribute>();
                if (hasId)
                {
                    if (foundIdAttribute)
                        throw new InvalidImplementationException(type,
                            typeof(IPrototype),
                            $"Found two {nameof(IdDataFieldAttribute)}");

                    foundIdAttribute = true;
                }

                if (hasParent)
                {
                    if (foundParentAttribute)
                        throw new InvalidImplementationException(type,
                            typeof(IInheritingPrototype),
                            $"Found two {nameof(ParentDataFieldAttribute)}");

                    foundParentAttribute = true;
                }

                if (hasId && hasParent)
                    throw new InvalidImplementationException(type,
                        typeof(IPrototype),
                        $"Prototype {type} has the Id- & ParentDatafield on single member {info.Name}");

                if (info.HasAttribute<AbstractDataFieldAttribute>())
                {
                    if (foundAbstractAttribute)
                        throw new InvalidImplementationException(type,
                            typeof(IInheritingPrototype),
                            $"Found two {nameof(AbstractDataFieldAttribute)}");

                    foundAbstractAttribute = true;
                }
            }

            if (!foundIdAttribute)
                throw new InvalidImplementationException(type,
                    typeof(IPrototype),
                    $"Did not find any member annotated with the {nameof(IdDataFieldAttribute)}");

            if (type.IsAssignableTo(typeof(IInheritingPrototype)) && (!foundParentAttribute || !foundAbstractAttribute))
                throw new InvalidImplementationException(type,
                    typeof(IInheritingPrototype),
                    $"Did not find any member annotated with the {nameof(ParentDataFieldAttribute)} and/or {nameof(AbstractDataFieldAttribute)}");

            _prototypeTypes[attribute.Type] = type;
            _prototypePriorities[type] = attribute.LoadPriority;

            if (typeof(IPrototype).IsAssignableFrom(type))
            {
                _prototypes[type] = new Dictionary<string, IPrototype>();
                _prototypeResults[type] = new Dictionary<string, MappingDataNode>();
                if (typeof(IInheritingPrototype).IsAssignableFrom(type))
                    _inheritanceTrees[type] = new MultiRootInheritanceGraph<string>();
            }
        }

        public event Action<PrototypesReloadedEventArgs>? PrototypesReloaded;
    }
}
