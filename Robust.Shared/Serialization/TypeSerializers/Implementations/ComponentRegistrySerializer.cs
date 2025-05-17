using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
    public sealed class ComponentRegistrySerializer : ITypeSerializer<ComponentRegistry, SequenceDataNode>, ITypeInheritanceHandler<ComponentRegistry, SequenceDataNode>, ITypeCopier<ComponentRegistry>
    {
        public ComponentRegistry Read(ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<ComponentRegistry>? instanceProvider = null)
        {
            var factory = dependencies.Resolve<IComponentFactory>();
            var components = instanceProvider != null ? instanceProvider() : new ComponentRegistry();
            var comps = new Dictionary<string, MappingDataNode?>(node.Sequence.Count);

            foreach (var sequenceEntry in node.Sequence)
            {
                var componentMapping = (MappingDataNode) sequenceEntry;
                var compType = ((ValueDataNode) componentMapping.Get("type")).Value;

                if (factory.IsAlias(compType, out var aliasType))
                    comps.TryAdd(aliasType, null);

                if (comps.TryAdd(compType, componentMapping))
                    continue;

                // Might already exist in the dictionary if a previous entry was an alias for this entry
                if (comps[compType] == null)
                {
                    comps[compType] = componentMapping;
                    continue;
                }

                // duplicate entry.
                dependencies
                    .Resolve<ILogManager>()
                    .GetSawmill(SerializationManager.LogCategory)
                    .Error($"Component of type '{compType}' defined twice in prototype!");
            }

            MergeAliases(comps, factory);

            foreach (var (compType, componentMapping) in comps)
            {
                DebugTools.Assert(!factory.IsAlias(compType, out _));

                // See if type exists to detect errors.
                switch (factory.GetComponentAvailability(compType))
                {
                    case ComponentAvailability.Available:
                        break;

                    case ComponentAvailability.Ignore:
                        continue;

                    case ComponentAvailability.Unknown:
                        dependencies
                            .Resolve<ILogManager>()
                            .GetSawmill(SerializationManager.LogCategory)
                            .Error($"Unknown component '{compType}' in prototype!");
                        continue;
                }

                // TODO allocations
                // just ignore the "type" key when deserializing comps
                // I.e., remove this Copy()
                var copy = componentMapping!.Copy();
                copy.Remove("type");

                var type = factory.GetRegistration(compType).Type;
                var read = (IComponent)serializationManager.Read(type, copy, hookCtx, context)!;

                components[compType] = new ComponentRegistryEntry(read, copy);
            }

            var referenceTypes = new List<CompIdx>();
            // Assert that there are no conflicting component references.
            foreach (var componentName in components.Keys)
            {
                var registration = factory.GetRegistration(componentName);
                var compType = registration.Idx;

                if (referenceTypes.Contains(compType))
                {
                    throw new InvalidOperationException(
                        $"Duplicate component reference in prototype: '{compType}'");
                }

                referenceTypes.Add(compType);
            }

            return components;
        }

        internal static void MergeAliases(Dictionary<string, MappingDataNode?> data, IComponentFactory factory)
        {
            foreach (var name in data.Keys)
            {
                if (!factory.TryGetAliases(name, out var aliases))
                {
                    DebugTools.AssertNotNull(data[name]);
                    continue;
                }

                var existing = data[name];
                var copy = false;

                foreach (var alias in aliases)
                {
                    if (!data.Remove(alias, out var removed))
                        continue;

                    if (existing == null)
                    {
                        existing = removed;
                        continue;
                    }

                    if (!copy)
                    {
                        existing = existing.Copy();
                        copy = true;
                    }

                    // Need to skip duplicate, as the "type" key will always be duplicated.
                    existing.Insert(removed!, skipDuplicates: true);
                }

                DebugTools.AssertNotNull(existing);
                data[name] = existing;
            }
        }

        public ValidationNode Validate(ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            var factory = dependencies.Resolve<IComponentFactory>();
            var components = new Dictionary<string, MappingDataNode?>(node.Sequence.Count);
            var list = new List<ValidationNode>();

            foreach (var sequenceEntry in node.Sequence)
            {
                if (sequenceEntry is not MappingDataNode componentMapping)
                {
                    list.Add(new ErrorNode(sequenceEntry, $"Expected {nameof(MappingDataNode)}"));
                    continue;
                }

                var name = ((ValueDataNode) componentMapping.Get("type")).Value;

                if (factory.IsAlias(name, out var aliasType))
                    components.TryAdd(aliasType, null);

                if (components.TryAdd(name, componentMapping))
                    continue;

                // Might already exist in the dictionary if a previous entry was an alias for this entry
                if (components[name] == null)
                {
                    components[name] = componentMapping;
                    continue;
                }

                // duplicate entry.
                list.Add(new ErrorNode(componentMapping, $"Duplicate Component {name}."));
            }

            MergeAliases(components, factory);

            foreach (var (compType, componentMappingNullable) in components)
            {
                var componentMapping = componentMappingNullable!;
                DebugTools.Assert(!factory.IsAlias(compType, out _));

                // See if type exists to detect errors.
                switch (factory.GetComponentAvailability(compType))
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

                var copy = componentMapping.Copy()!;
                copy.Remove("type");

                var type = factory.GetRegistration(compType).Type;

                list.Add(serializationManager.ValidateNode(type, copy, context));
            }

            var referenceTypes = new List<CompIdx>();

            // Assert that there are no conflicting component references.
            foreach (var componentName in components.Keys)
            {
                var registration = factory.GetRegistration(componentName);
                var compType = registration.Idx;

                if (referenceTypes.Contains(compType))
                {
                    return new ErrorNode(node, "Duplicate ComponentReference.");
                }

                referenceTypes.Add(compType);
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
                target.Add(id, serializationManager.CreateCopy(component, context, notNullableOverride: true));
            }
        }

        public SequenceDataNode PushInheritance(ISerializationManager serializationManager, SequenceDataNode child,
            SequenceDataNode parent,
            IDependencyCollection dependencies, ISerializationContext? context)
        {
            var componentFactory = dependencies.Resolve<IComponentFactory>();
            var newCompReg = child.Copy();
            var newCompRegDict = ToTypeIndexedDictionary(newCompReg, componentFactory);
            var parentDict = ToTypeIndexedDictionary(parent, componentFactory);

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

        private Dictionary<ComponentRegistration, int> ToTypeIndexedDictionary(SequenceDataNode node, IComponentFactory componentFactory)
        {
            var dict = new Dictionary<ComponentRegistration, int>();
            for (var i = 0; i < node.Count; i++)
            {
                var mapping = (MappingDataNode)node[i];
                var type = mapping.Get<ValueDataNode>("type").Value;

                var availability = componentFactory.GetComponentAvailability(type);
                if(availability == ComponentAvailability.Ignore) continue;

                if (!dict.TryAdd(componentFactory.GetRegistration(type), i))
                {
                    throw new ArgumentException(
                        $"Tried to add {type} to sequence nodes but already exists. Check for duplicate components!");
                }
            }

            return dict;
        }
    }
}
