using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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
    public sealed class ComponentRegistrySerializer : ITypeSerializer<ComponentRegistry, SequenceDataNode>, ITypeInheritanceHandler<ComponentRegistry, SequenceDataNode>
    {
        public ComponentRegistry Read(ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null, ComponentRegistry? components = null)
        {
            var factory = dependencies.Resolve<IComponentFactory>();
            components ??= new ComponentRegistry();

            foreach (var componentMapping in node.Sequence.Cast<MappingDataNode>())
            {
                string compType = ((ValueDataNode) componentMapping.Get("type")).Value;
                // See if type exists to detect errors.
                switch (factory.GetComponentAvailability(compType))
                {
                    case ComponentAvailability.Available:
                        break;

                    case ComponentAvailability.Ignore:
                        continue;

                    case ComponentAvailability.Unknown:
                        Logger.ErrorS(SerializationManager.LogCategory, $"Unknown component '{compType}' in prototype!");
                        continue;
                }

                // Has this type already been added?
                if (components.Keys.Contains(compType))
                {
                    Logger.ErrorS(SerializationManager.LogCategory, $"Component of type '{compType}' defined twice in prototype!");
                    continue;
                }

                var copy = componentMapping.Copy()!;
                copy.Remove("type");

                var type = factory.GetRegistration(compType).Type;
                var read = (IComponent)serializationManager.Read(type, copy, skipHook: skipHook)!;

                components[compType] = read;
            }

            var referenceTypes = new List<Type>();
            // Assert that there are no conflicting component references.
            foreach (var componentName in components.Keys)
            {
                var registration = factory.GetRegistration(componentName);
                foreach (var compType in registration.References)
                {
                    if (referenceTypes.Contains(compType))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate component reference in prototype: '{compType}'");
                    }

                    referenceTypes.Add(compType);
                }
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

            foreach (var componentMapping in node.Sequence.Cast<MappingDataNode>())
            {
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
                if (components.Keys.Contains(compType))
                {
                    list.Add(new ErrorNode(componentMapping, "Duplicate Component."));
                    continue;
                }

                var copy = componentMapping.Copy()!;
                copy.Remove("type");

                var type = factory.GetRegistration(compType).Type;

                list.Add(serializationManager.ValidateNode(type, copy, context));
            }

            var referenceTypes = new List<Type>();

            // Assert that there are no conflicting component references.
            foreach (var componentName in components.Keys)
            {
                var registration = factory.GetRegistration(componentName);

                foreach (var compType in registration.References)
                {
                    if (referenceTypes.Contains(compType))
                    {
                        return new ErrorNode(node, "Duplicate ComponentReference.");
                    }

                    referenceTypes.Add(compType);
                }
            }

            return new ValidatedSequenceNode(list);
        }

        public DataNode Write(ISerializationManager serializationManager, ComponentRegistry value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var compSequence = new SequenceDataNode();
            foreach (var (type, component) in value)
            {
                var node = serializationManager.WriteValue(component.GetType(), component, alwaysWrite, context);
                if (node is not MappingDataNode mapping) throw new InvalidNodeTypeException();

                mapping.Add("type", new ValueDataNode(type));
                compSequence.Add(mapping);
            }

            return compSequence;
        }

        [MustUseReturnValue]
        public ComponentRegistry Copy(ISerializationManager serializationManager, ComponentRegistry source,
            ComponentRegistry target, bool skipHook, ISerializationContext? context = null)
        {
            target.Clear();
            target.EnsureCapacity(source.Count);

            foreach (var (id, component) in source)
            {
                target.Add(id, serializationManager.CreateCopy(component, context)!);
            }

            return target;
        }

        public SequenceDataNode PushInheritance(ISerializationManager serializationManager, SequenceDataNode child,
            SequenceDataNode parent,
            IDependencyCollection dependencies, ISerializationContext context)
        {
            var componentFactory = dependencies.Resolve<IComponentFactory>();
            var newCompReg = child.Copy();
            var newCompRegDict = ToTypeIndexedDictionary(newCompReg, componentFactory);
            var parentDict = ToTypeIndexedDictionary(parent, componentFactory);

            foreach (var (reg, mapping) in parentDict)
            {
                if (newCompRegDict.TryFirstOrNull(childReg => reg.References.Any(x => childReg.Key.References.Contains(x)), out var entry))
                {
                    newCompReg[entry.Value.Value] = serializationManager.PushCompositionWithGenericNode(reg.Type,
                        new[] { parent[mapping] }, newCompReg[entry.Value.Value], context);
                }
                else
                {
                    newCompReg.Add(parent[mapping]);
                    newCompRegDict[reg] = newCompReg.Count-1;
                }
            }

            return newCompReg;
        }

        private Dictionary<IComponentRegistration, int> ToTypeIndexedDictionary(SequenceDataNode node, IComponentFactory componentFactory)
        {
            var dict = new Dictionary<IComponentRegistration, int>();
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
