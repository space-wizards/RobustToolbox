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
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;

        public DeserializationResult<ComponentRegistry> Read(SequenceDataNode node,
            ISerializationContext? context = null)
        {
            var components = new ComponentRegistry();

            foreach (var componentMapping in node.Sequence.Cast<MappingDataNode>())
            {
                string compType = ((ValueDataNode)componentMapping.GetNode("type")).Value;
                // See if type exists to detect errors.
                switch (_componentFactory.GetComponentAvailability(compType))
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

                var type = _componentFactory.GetRegistration(compType).Type;
                var data = _serializationManager.ReadValue<IComponent>(type, copy) ??
                           throw new NullReferenceException();

                components[compType] = data;
            }

            var referenceTypes = new List<Type>();
            // Assert that there are no conflicting component references.
            foreach (var componentName in components.Keys)
            {
                var registration = _componentFactory.GetRegistration(componentName);
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

            return DeserializationResult.Value(components);
        }

        public DataNode Write(ComponentRegistry value,
            bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var compSequence = new SequenceDataNode();
            foreach (var (type, component) in value)
            {
                var node = _serializationManager.WriteValue(component.GetType(), component, alwaysWrite, context);
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
        public ComponentRegistry Copy(ComponentRegistry source, ComponentRegistry target)
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
