using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using static Robust.Shared.Prototypes.EntityPrototype;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class ComponentRegistrySerializer : ITypeSerializer<ComponentRegistry, SequenceDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager,
            SequenceDataNode node,
            IDependencyCollection dependencies,
            bool skipHook,
            ISerializationContext? context = null)
        {
            var factory = dependencies.Resolve<IComponentFactory>();
            var components = new ComponentRegistry();
            var mappings = new Dictionary<DeserializationResult, DeserializationResult>();

            foreach (var componentMapping in node.Sequence.Cast<MappingDataNode>())
            {
                string compType = ((ValueDataNode) componentMapping.GetNode("type")).Value;
                // See if type exists to detect errors.
                switch (factory.GetComponentAvailability(compType))
                {
                    case ComponentAvailability.Available:
                        break;

                    case ComponentAvailability.Ignore:
                        continue;

                    case ComponentAvailability.Unknown:
                        Logger.Error(SerializationManager.LogCategory, $"Unknown component '{compType}' in prototype!");
                        continue;
                }

                // Has this type already been added?
                if (components.Keys.Contains(compType))
                {
                    Logger.Error(SerializationManager.LogCategory, $"Component of type '{compType}' defined twice in prototype!");
                    continue;
                }

                var copy = (componentMapping.Copy() as MappingDataNode)!;
                copy.RemoveNode("type");

                var type = factory.GetRegistration(compType).Type;
                var read = serializationManager.ReadWithValueOrThrow<IComponent>(type, copy, skipHook: skipHook);

                components[compType] = read.value;
                mappings.Add(DeserializationResult.Value(compType), read.result);
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

            return new DeserializedComponentRegistry(components, mappings);
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
                string compType = ((ValueDataNode) componentMapping.GetNode("type")).Value;
                // See if type exists to detect errors.
                switch (factory.GetComponentAvailability(compType))
                {
                    case ComponentAvailability.Available:
                        break;

                    case ComponentAvailability.Ignore:
                        list.Add(new ValidatedValueNode(componentMapping));
                        continue;

                    case ComponentAvailability.Unknown:
                        list.Add(new ErrorNode(componentMapping, "Unknown ComponentType."));
                        continue;
                }

                // Has this type already been added?
                if (components.Keys.Contains(compType))
                {
                    list.Add(new ErrorNode(componentMapping, "Duplicate Component."));
                    continue;
                }

                var copy = (componentMapping.Copy() as MappingDataNode)!;
                copy.RemoveNode("type");

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
//                if (mapping.Children.Count != 0)
//                {
                    mapping.AddNode("type", new ValueDataNode(type));
                    compSequence.Add(mapping);
//                }
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
    }
}
