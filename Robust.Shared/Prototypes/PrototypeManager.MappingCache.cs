using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Robust.Shared.Prototypes;

public abstract partial class PrototypeManager
{
    private Dictionary<string, FrozenDictionary<string, MappingDataNode>> _prototypeDataCache = new();
    private readonly object _mappingCacheLock = new();
    private Dictionary<Type, Dictionary<string, MappingDataNode>> _resolvedMappingCache = new();
    private int _mappingCacheDepth;

    public IDisposable BeginScope()
    {
        lock (_mappingCacheLock)
        {
            _mappingCacheDepth++;
            return new MappingCacheScope(this);
        }
    }

    private void InvalidateMappingCaches()
    {
        lock (_mappingCacheLock)
        {
            _resolvedMappingCache = new();
            _prototypeDataCache = new();
        }
    }

    private sealed class MappingCacheScope(PrototypeManager owner) : IDisposable
    {
        private PrototypeManager? _owner = owner;

        public void Dispose()
        {
            var manager = Interlocked.Exchange(ref _owner, null);
            if (manager == null)
                return;

            // We always drop the cache... but only if this is the outermost cache.
            // If it's nested then just leave it for now just in case.
            lock (manager._mappingCacheLock)
            {
                if (--manager._mappingCacheDepth == 0)
                    manager.InvalidateMappingCaches();
            }
        }
    }

    private IDisposable BeginPrototypeLoadScope()
    {
        InvalidateMappingCaches();
        _prototypeLoadContext?.ClearComponentCache();
        return new PrototypeLoadScope(this, _prototypeLoadContext, BeginScope());
    }

    private sealed class PrototypeLoadScope(
        PrototypeManager owner,
        PrototypeLoadContext? context,
        IDisposable mappings) : IDisposable
    {
        public void Dispose()
        {
            try
            {
                context?.ClearComponentCache();
                DiscardResolvedMappings(owner._kinds.Values);
            }
            finally
            {
                mappings.Dispose();
            }
        }
    }

    /// <summary>
    /// Returns an effective mapping, retaining it only within a prototype mapping scope.
    /// </summary>
    private bool TryGetResolvedMapping(Type kind, KindData kindData, string id,
        [NotNullWhen(true)] out MappingDataNode? mapping)
    {
        lock (_mappingCacheLock)
        {
            if (kindData.Results.TryGetValue(id, out mapping))
                return true;

            if (!kindData.RawResults.TryGetValue(id, out var raw))
            {
                mapping = null;
                return false;
            }

            if (kindData.Inheritance == null || !kindData.Inheritance.TryGetParents(id, out _))
            {
                mapping = raw;
                return true;
            }

            var resolved = _mappingCacheDepth > 0
                ? _resolvedMappingCache.GetOrNew(kind)
                : new Dictionary<string, MappingDataNode>();
            return TryComposeMapping(kind, kindData, id, resolved, out mapping);
        }
    }

    private bool TryComposeMapping(Type kind, KindData kindData, string id,
        Dictionary<string, MappingDataNode> resolved, [NotNullWhen(true)] out MappingDataNode? mapping)
    {
        if (kindData.Results.TryGetValue(id, out mapping) || resolved.TryGetValue(id, out mapping))
            return true;

        if (!kindData.RawResults.TryGetValue(id, out var raw))
        {
            mapping = null;
            return false;
        }

        var tree = kindData.Inheritance!;
        if (!tree.TryGetParents(id, out var parents))
        {
            mapping = raw;
            resolved.Add(id, mapping);
            return true;
        }

        MappingDataNode result;
        if (parents.Length == 1)
        {
            if (!TryComposeMapping(kind, kindData, parents[0], resolved, out var parent))
            {
                mapping = null;
                return false;
            }

            result = _serializationManager.PushCompositionWithGenericNode(kind, parent, raw);
        }
        else
        {
            var parentMaps = new MappingDataNode[parents.Length];
            for (var i = 0; i < parents.Length; i++)
            {
                if (!TryComposeMapping(kind, kindData, parents[i], resolved, out var parent))
                {
                    mapping = null;
                    return false;
                }

                parentMaps[i] = parent;
            }

            result = _serializationManager.PushCompositionWithGenericNode(kind, parentMaps, raw);
        }

        resolved.Add(id, result);
        mapping = result;
        return true;
    }

    private static void DiscardResolvedMappings(IEnumerable<KindData> kinds)
    {
        foreach (var kind in kinds)
        {
            if (kind.Inheritance != null)
                kind.Results = new Dictionary<string, MappingDataNode>();
        }
    }

    public IReadOnlyDictionary<string, MappingDataNode> GetPrototypeData(EntityPrototype prototype)
    {
        lock (_mappingCacheLock)
        {
            if (_mappingCacheDepth > 0 && _prototypeDataCache.TryGetValue(prototype.ID, out var cached))
                return cached;

            var componentData = new Dictionary<string, MappingDataNode>();
            if (TryGetMapping<EntityPrototype>(prototype.ID, out var mapping)
                && mapping.TryGet<SequenceDataNode>("components", out var components))
            {
                foreach (var component in components)
                {
                    if (component is not MappingDataNode componentMapping
                        || !componentMapping.TryGet<ValueDataNode>("type", out var type))
                        continue;

                    componentData[type.Value] = componentMapping.CopyNoType();
                }
            }

            var data = componentData.ToFrozenDictionary();
            if (_mappingCacheDepth > 0)
                _prototypeDataCache[prototype.ID] = data;
            return data;
        }
    }

}
