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

            foreach (var sequenceEntry in node.Sequence)
            {
                var componentMapping = (MappingDataNode)sequenceEntry;
                string compType = ((ValueDataNode) componentMapping.Get("type")).Value;
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

                // Has this type already been added?
                if (components.ContainsKey(compType))
                {
                    dependencies
                        .Resolve<ILogManager>()
                        .GetSawmill(SerializationManager.LogCategory)
                        .Error($"Component of type '{compType}' defined twice in prototype!");
                    continue;
                }

                var copy = componentMapping.Copy()!;
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

        public ValidationNode Validate(ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            var factory = dependencies.Resolve<IComponentFactory>();
            var components = new ComponentRegistry();
            var list = new List<ValidationNode>();

            foreach (var sequenceEntry in node.Sequence)
            {
                if (sequenceEntry is not MappingDataNode componentMapping)
                {
                    list.Add(new ErrorNode(sequenceEntry, $"Expected {nameof(MappingDataNode)}"));
                    continue;
                }
                string compType = ((ValueDataNode) componentMapping.Get("type")).Value;
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

                // Has this type already been added?
                if (components.ContainsKey(compType))
                {
                    list.Add(new ErrorNode(componentMapping, "Duplicate Component."));
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
                dict.Add(componentFactory.GetRegistration(type), i);
            }

            return dict;
        }
    }
}
