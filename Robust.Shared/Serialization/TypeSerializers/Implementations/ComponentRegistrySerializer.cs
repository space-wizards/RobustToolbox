using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Robust.Shared.Collections;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;
using static Robust.Shared.Prototypes.EntityPrototype;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public sealed partial class ComponentRegistrySerializer : BaseTypeSerializer, ITypeSerializer<ComponentRegistry, SequenceDataNode>, ITypeInheritanceHandler<ComponentRegistry, SequenceDataNode>, ITypeCopier<ComponentRegistry>,
        IPostInjectInit
    {
        [Dependency] private IDynamicTypeFactory _dynamicTypeFactory = default!;
        [Dependency] private IComponentFactory _factory = default!;

        private IDynamicTypeFactoryInternal _dynamicTypeFactoryInternal = default!;

        internal bool CacheComponents;
        private readonly ConcurrentDictionary<MappingDataNode, Component> _cache = new(new ComponentMappingComparer());

        public ComponentRegistry Read(ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<ComponentRegistry>? instanceProvider = null)
        {
            var components = instanceProvider != null ? instanceProvider() : new ComponentRegistry();
            var referenceTypes = node.Count <= 1024 ? stackalloc CompIdx[node.Count] : new CompIdx[node.Count];
            var refIdx = 0;

            foreach (var sequenceEntry in node.Sequence)
            {
                var componentMapping = (MappingDataNode)sequenceEntry;

                if (!componentMapping.TryGet("type", out ValueDataNode? typeNode))
                {
                    if (componentMapping.Tag == PrototypeManager.PartialModifiedTag)
                        continue;

                    throw new KeyNotFoundException("The given key 'type' was not present in the dictionary.");
                }

                var compType = typeNode.Value;
                // See if type exists to detect errors.
                switch (_factory.GetComponentAvailability(compType))
                {
                    case ComponentAvailability.Available:
                        break;

                    case ComponentAvailability.Ignore:
                        continue;

                    case ComponentAvailability.Unknown:
                        Log.Error($"Unknown component '{compType}' in prototype!");
                        continue;
                }

                var registration = _factory.GetRegistration(compType);
                var compIdx = registration.Idx;

                // Has this type already been added?
                if (referenceTypes[..refIdx].Contains(compIdx))
                {
                    throw new InvalidOperationException(
                        $"Duplicate component reference in prototype: '{compIdx}'");
                }

                referenceTypes[refIdx++] = compIdx;

                Component comp;
                var tuple = (inst: this, type: registration.Type, serialization: serializationManager, hookCtx, context);
                if (CacheComponents)
                {
                    comp = _cache.GetOrAdd(
                        componentMapping,
                        static (componentMapping, tuple) => ReadComponent(
                            tuple.inst,
                            componentMapping,
                            tuple.type,
                            tuple.serialization,
                            tuple.hookCtx,
                            tuple.context
                        ),
                        tuple
                    );
                }
                else
                {
                    comp = ReadComponent(
                        this,
                        componentMapping,
                        registration.Type,
                        serializationManager,
                        hookCtx,
                        context
                    );
                }

                // The full YAML mapping is already retained by PrototypeManager.
                components[compType] = new ComponentRegistryEntry(comp);
            }

            return components;
        }

        public ValidationNode Validate(ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            var componentNames = new HashSet<string>();
            var list = new List<ValidationNode>();
            var referenceTypes = node.Count <= 1024 ? stackalloc CompIdx[node.Count] : new CompIdx[node.Count];
            var refIdx = 0;

            foreach (var sequenceEntry in node.Sequence)
            {
                if (sequenceEntry is not MappingDataNode componentMapping)
                {
                    list.Add(new ErrorNode(sequenceEntry, $"Expected {nameof(MappingDataNode)}"));
                    continue;
                }

                if (!componentMapping.TryGet("type", out ValueDataNode? typeNode))
                {
                    if (componentMapping.Tag == PrototypeManager.PartialModifiedTag)
                        continue;

                    throw new KeyNotFoundException("The given key 'type' was not present in the dictionary.");
                }

                string compType = typeNode.Value;
                // See if type exists to detect errors.
                switch (_factory.GetComponentAvailability(compType))
                {
                    case ComponentAvailability.Available:
                        break;

                    case ComponentAvailability.Ignore:
                        list.Add(new ValidatedValueNode(componentMapping));
                        continue;

                    case ComponentAvailability.Unknown:
                        list.Add(new ErrorNode(componentMapping, $"Unknown component type {compType}."));
                        continue;
                }

                // Has this type already been added?
                if (!componentNames.Add(compType))
                {
                    list.Add(new ErrorNode(componentMapping, "Duplicate Component."));
                    continue;
                }

                var registration = _factory.GetRegistration(compType);
                var compIdx = registration.Idx;

                if (referenceTypes[..refIdx].Contains(compIdx))
                {
                    list.Add(new ErrorNode(componentMapping, "Duplicate ComponentReference."));
                    continue;
                }

                referenceTypes[refIdx++] = compIdx;

                var copy = componentMapping.CopyNoType();
                list.Add(serializationManager.ValidateNode(registration.Type, copy, context));
            }

            return new ValidatedSequenceNode(list);
        }

        public DataNode Write(ISerializationManager serializationManager, ComponentRegistry value,
            IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var compSequence = new SequenceDataNode();
            foreach (var (type, component) in value)
            {
                var node = serializationManager.WriteValue(
                    component.Component.GetType(),
                    component.Component,
                    alwaysWrite,
                    context);

                if (node is not MappingDataNode mapping) throw new InvalidNodeTypeException();

                mapping.Add("type", new ValueDataNode(type));
                compSequence.Add(mapping);
            }

            return compSequence;
        }

        public void CopyTo(ISerializationManager serializationManager, ComponentRegistry source, ref ComponentRegistry target,
            IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
        {
            target.Clear();
            target.EnsureCapacity(source.Count);

            foreach (var (id, component) in source)
            {
                var copy = serializationManager.CreateCopy(component.Component, context, notNullableOverride: true);
                target.Add(id, new ComponentRegistryEntry(copy));
            }
        }

        public SequenceDataNode PushInheritance(ISerializationManager serializationManager, SequenceDataNode child,
            SequenceDataNode parent,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            var newCompReg = child.Copy();
            var newCompRegDict = ToTypeIndexedDictionary(newCompReg);
            var parentDict = ToTypeIndexedDictionary(parent);

            foreach (var (reg, mapping) in parentDict)
            {
                foreach (var (childReg, idx) in newCompRegDict)
                {
                    if (childReg.Idx.Equals(reg.Idx))
                    {
                        newCompReg[idx] = serializationManager.PushCompositionWithGenericNode(
                            reg.Type,
                            parent[mapping],
                            newCompReg[idx],
                            context);

                        goto found;
                    }
                }

                // Not found.

                newCompReg.Add(parent[mapping]);
                newCompRegDict[reg] = newCompReg.Count-1;

                found: ;
            }

            return newCompReg;
        }

        private Dictionary<ComponentRegistration, int> ToTypeIndexedDictionary(SequenceDataNode node)
        {
            var dict = new Dictionary<ComponentRegistration, int>();
            for (var i = 0; i < node.Count; i++)
            {
                var mapping = (MappingDataNode)node[i];
                var type = mapping.Get<ValueDataNode>("type").Value;
                var availability = _factory.GetComponentAvailability(type);
                if (availability == ComponentAvailability.Ignore)
                    continue;

                dict.Add(_factory.GetRegistration(type), i);
            }

            return dict;
        }

        private static Component ReadComponent(
            ComponentRegistrySerializer inst,
            MappingDataNode mapping,
            Type type,
            ISerializationManager serializationManager,
            SerializationHookContext hookCtx,
            ISerializationContext? context)
        {
            var comp = (Component) inst._dynamicTypeFactoryInternal.CreateInstanceUnchecked(type, inject: false);
#pragma warning disable CS0618 // Type or member is obsolete
            comp = comp.Instantiate();
#pragma warning restore CS0618 // Type or member is obsolete
            comp.ReadComp(ref comp, mapping, serializationManager, hookCtx, context);
            SerializationManager.TryRunAfterHook(comp, hookCtx);
            return comp;
        }

        void IPostInjectInit.PostInject()
        {
            _dynamicTypeFactoryInternal = (IDynamicTypeFactoryInternal) _dynamicTypeFactory;
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
                => node.GetCanonicalHashCode();

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

        }
    }
}
