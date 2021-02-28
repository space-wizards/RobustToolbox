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
using static Robust.Shared.Prototypes.EntityPrototype;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class ComponentRegistrySerializer :ITypeSerializer<ComponentRegistry, SequenceDataNode>
    {
        public DeserializationResult<ComponentRegistry> Read(ISerializationManager serializationManager,
            SequenceDataNode node,
            ISerializationContext? context = null)
        {
            var factory = IoCManager.Resolve<IComponentFactory>();
            var components = new ComponentRegistry();
            var mappings = new Dictionary<DeserializationResult, DeserializationResult>();

            foreach (var componentMapping in node.Sequence.Cast<MappingDataNode>())
            {
                string compType = ((ValueDataNode)componentMapping.GetNode("type")).Value;
                // See if type exists to detect errors.
                switch (factory.GetComponentAvailability(compType))
                {
                    case ComponentAvailability.Available:
                        break;

                    case ComponentAvailability.Ignore:
                        continue;

                    case ComponentAvailability.Unknown:
                        Logger.Error($"Unknown component '{compType}' in prototype!");
                        continue;
                }

                // Has this type already been added?
                if (components.Keys.Contains(compType))
                {
                    Logger.Error($"Component of type '{compType}' defined twice in prototype!");
                    continue;
                }

                var copy = (componentMapping.Copy() as MappingDataNode)!;
                copy.RemoveNode("type");

                var type = factory.GetRegistration(compType).Type;
                var read = serializationManager.ReadWithValueOrThrow<IComponent>(type, copy);

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

            return new DeserializedDictionary<ComponentRegistry, string, IComponent>(components, mappings);
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
                if (mapping.Children.Count != 0)
                {
                    mapping.AddNode("type", new ValueDataNode(type));
                    compSequence.Add(mapping);
                }
            }

            return compSequence;
        }

        [MustUseReturnValue]
        public ComponentRegistry Copy(ISerializationManager serializationManager, ComponentRegistry source,
            ComponentRegistry target)
        {
            target.Clear();
            target.EnsureCapacity(source.Count);

            foreach (var (id, component) in source)
            {
                target.Add(id, component);
            }

            return target;
        }
    }
}
