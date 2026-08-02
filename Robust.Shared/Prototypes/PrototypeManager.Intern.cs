using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.Shared.Prototypes
{
    public abstract partial class PrototypeManager
    {
        // Component mappings are immutable after prototype loading finishes. Keeping one component entry for every
        // distinct mapping substantially reduces the memory needed by EntityPrototype instances, especially for
        // components inherited unchanged by many children.
        // TODO: Allow other parented prototype types to opt into this cache
        private static readonly ComponentMappingComparer ComponentMappingNodeComparer = new();

        private FrozenDictionary<MappingDataNode, EntityPrototype.ComponentRegistryEntry> _entityComponentCache =
            FrozenDictionary<MappingDataNode, EntityPrototype.ComponentRegistryEntry>.Empty;

        /// <summary>
        /// Store entity prototype component entries by their resolved YAML mapping.
        ///
        /// Components are copied before they are added to an entity, so sharing shouldn't mutate these.
        /// The engine doesn't have an API to actually expose them as readonly but we just trust.
        /// </summary>
        private void RebuildEntityComponentCache()
        {
            if (!_kinds.TryGetValue(typeof(EntityPrototype), out var entityKind))
            {
                _entityComponentCache = FrozenDictionary<MappingDataNode, EntityPrototype.ComponentRegistryEntry>.Empty;
                return;
            }

            var cache = new Dictionary<MappingDataNode, EntityPrototype.ComponentRegistryEntry>(ComponentMappingNodeComparer);

            foreach (var (id, mapping) in entityKind.Results)
            {
                if (!entityKind.Instances.TryGetValue(id, out var instance)
                    || instance is not EntityPrototype prototype
                    || !mapping.TryGet<SequenceDataNode>("components", out var componentMappings))
                {
                    continue;
                }

                foreach (var componentNode in componentMappings)
                {
                    if (componentNode is not MappingDataNode componentMapping
                        || !componentMapping.TryGet<ValueDataNode>("type", out var typeNode)
                        || !prototype.Components.TryGetValue(typeNode.Value, out var component))
                    {
                        continue;
                    }

                    if (!cache.TryGetValue(componentMapping, out var canonical)
                        && !_entityComponentCache.TryGetValue(componentMapping, out canonical))
                    {
                        canonical = component;
                    }

                    cache.TryAdd(componentMapping, canonical);
                    prototype.Components[typeNode.Value] = canonical;
                }
            }

            _entityComponentCache = cache.ToFrozenDictionary(ComponentMappingNodeComparer);
        }

        private sealed class ComponentMappingComparer : IEqualityComparer<MappingDataNode>
        {
            public bool Equals(MappingDataNode? x, MappingDataNode? y)
            {
                if (ReferenceEquals(x, y))
                    return true;

                if (x == null || y == null || x.Count != y.Count || x.Tag != y.Tag)
                    return false;

                foreach (var (key, value) in x)
                {
                    if (!y.TryGet(key, out var other) || !DataNodesEqual(value, other))
                        return false;
                }

                return true;
            }

            public int GetHashCode(MappingDataNode node)
            {
                var entriesHash = 0;
                foreach (var (key, value) in node)
                {
                    entriesHash ^= HashCode.Combine(StringComparer.Ordinal.GetHashCode(key), GetDataNodeHashCode(value));
                }

                return HashCode.Combine(node.Tag, node.Count, entriesHash);
            }

            private static bool DataNodesEqual(DataNode x, DataNode y)
            {
                if (ReferenceEquals(x, y))
                    return true;

                if (x.GetType() != y.GetType() || x.Tag != y.Tag || x.IsNull != y.IsNull)
                    return false;

                return x switch
                {
                    ValueDataNode value => value.Value == ((ValueDataNode) y).Value,
                    SequenceDataNode sequence => SequenceEqual(sequence, (SequenceDataNode) y),
                    MappingDataNode mapping => MappingEqual(mapping, (MappingDataNode) y),
                    _ => false
                };
            }

            private static bool SequenceEqual(SequenceDataNode x, SequenceDataNode y)
            {
                if (x.Count != y.Count)
                    return false;

                for (var i = 0; i < x.Count; i++)
                {
                    if (!DataNodesEqual(x[i], y[i]))
                        return false;
                }

                return true;
            }

            private static bool MappingEqual(MappingDataNode x, MappingDataNode y)
            {
                if (x.Count != y.Count)
                    return false;

                foreach (var (key, value) in x)
                {
                    if (!y.TryGet(key, out var other) || !DataNodesEqual(value, other))
                        return false;
                }

                return true;
            }

            private static int GetDataNodeHashCode(DataNode node)
            {
                return node switch
                {
                    ValueDataNode value => HashCode.Combine(typeof(ValueDataNode), value.Tag, value.Value, value.IsNull),
                    SequenceDataNode sequence => GetSequenceHashCode(sequence),
                    MappingDataNode mapping => GetMappingHashCode(mapping),
                    _ => 0
                };
            }

            private static int GetSequenceHashCode(SequenceDataNode node)
            {
                var hash = new HashCode();
                hash.Add(typeof(SequenceDataNode));
                hash.Add(node.Tag);
                foreach (var child in node)
                {
                    hash.Add(GetDataNodeHashCode(child));
                }

                return hash.ToHashCode();
            }

            private static int GetMappingHashCode(MappingDataNode node)
            {
                var entriesHash = 0;
                foreach (var (key, value) in node)
                {
                    entriesHash ^= HashCode.Combine(StringComparer.Ordinal.GetHashCode(key), GetDataNodeHashCode(value));
                }

                return HashCode.Combine(typeof(MappingDataNode), node.Tag, node.Count, entriesHash);
            }
        }
    }
}
