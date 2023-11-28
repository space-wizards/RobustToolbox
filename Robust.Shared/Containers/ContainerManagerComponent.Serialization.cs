using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using System.Collections.Generic;

namespace Robust.Shared.Containers;

public sealed partial class ContainerManagerComponent
{
    [TypeSerializer]
    private sealed partial class Serializer : ITypeSerializer<ContainerManagerComponent, MappingDataNode>, ITypeCopier<ContainerManagerComponent>
    {
        public ContainerManagerComponent Read(
            ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<ContainerManagerComponent>? instanceProvider = null
        )
        {
            var component = instanceProvider is { } ? instanceProvider() : new();

            if (node.TryGet("containers", out var containersNode))
                serializationManager.Read(node, context, instanceProvider: () => component.Containers, notNullableOverride: true);

            foreach (var (id, container) in component.Containers)
            {
                (container.Owner, container.Manager, container.ID) = (component.Owner, component, id);
            }

            return component;
        }

        public ValidationNode Validate(
            ISerializationManager serializationManager,
            MappingDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null
        )
        {
            var validation = new Dictionary<ValidationNode, ValidationNode>();

            foreach (var (keyNode, valNode) in node)
            {
                if (keyNode is not ValueDataNode keyValNode)
                {
                    validation.Add(
                        new ErrorNode(keyNode, $"Encountered {keyNode.GetType().Name} key while validating {nameof(ContainerManagerComponent)} YAML"),
                        new ErrorNode(valNode, $"Indexed by {keyNode.GetType().Name} key while validating {nameof(ContainerManagerComponent)} YAML")
                    );
                    continue;
                }

                if (keyValNode.Value == "containers")
                {
                    validation.Add(
                        new ValidatedValueNode(keyValNode),
                        serializationManager.ValidateNode<Dictionary<string, BaseContainer>>(valNode, context)
                    );
                    continue;
                }

                validation.Add(
                    new ErrorNode(keyNode, $"Encountered unrecognized key \"{keyValNode.Value}\" while validating {nameof(ContainerManagerComponent)} YAML"),
                    new ErrorNode(valNode, $"Indexed by unrecognized key \"{keyValNode.Value}\" while validating {nameof(ContainerManagerComponent)} YAML")
                );
            }

            return new ValidatedMappingNode(validation);
        }

        public DataNode Write(
            ISerializationManager serializationManager,
            ContainerManagerComponent value,
            IDependencyCollection dependencies,
            bool alwaysWrite = false,
            ISerializationContext? context = null
        )
        {
            var node = new MappingDataNode();

            if (alwaysWrite || value.Containers.Count > 0)
                node.Add("containers", serializationManager.WriteValue(value.Containers, alwaysWrite, context, notNullableOverride: true));

            return node;
        }

        public void CopyTo(
            ISerializationManager serializationManager,
            ContainerManagerComponent source,
            ref ContainerManagerComponent target,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null
        )
        {
            serializationManager.CopyTo(source.Containers, ref target.Containers, context, notNullableOverride: true);

            foreach (var (id, container) in target.Containers)
            {
                (container.Owner, container.Manager, container.ID) = (target.Owner, target, id);
            }
        }
    }
}
