using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

[TypeSerializer]
public sealed class ComponentFilterSerializer : ITypeSerializer<ComponentFilter, SequenceDataNode>, ITypeInheritanceHandler<ComponentFilter, SequenceDataNode>, ITypeCopier<ComponentFilter>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var factory = dependencies.Resolve<IComponentFactory>();
        var filter = new ComponentFilter();
        var list = new List<ValidationNode>();

        foreach (var seqEntry in node)
        {
            if (seqEntry is not ValueDataNode value)
            {
                list.Add(new ErrorNode(seqEntry, "Expected a single, scalar value."));
                continue;
            }

            var compType = value.Value;

            switch (factory.GetComponentAvailability(compType))
            {
                case ComponentAvailability.Available:
                    break;

                case ComponentAvailability.Ignore:
                    list.Add(new ValidatedValueNode(seqEntry));
                    continue;

                case ComponentAvailability.Unknown:
                    list.Add(new ErrorNode(seqEntry, $"Unknown component type {compType}."));
                    continue;
            }

            if (!filter.Add(factory, compType))
            {
                list.Add(new ErrorNode(seqEntry, "Duplicate Component."));
                continue;
            }

            list.Add(new ValidatedValueNode(seqEntry));
        }

        var referenceTypes = new List<CompIdx>();

        // Assert that there are no conflicting component references.
        foreach (var component in filter)
        {
            var registration = factory.GetRegistration(component);
            var compType = registration.Idx;

            if (referenceTypes.Contains(compType))
            {
                return new ErrorNode(node, "Contains a duplicate component reference.");
            }

            referenceTypes.Add(compType);
        }

        return new ValidatedSequenceNode(list);
    }

    public ComponentFilter Read(
        ISerializationManager serializationManager,
        SequenceDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<ComponentFilter>? instanceProvider = null)
    {
        var factory = dependencies.Resolve<IComponentFactory>();
        var filter = new ComponentFilter();

        foreach (var seqEntry in node)
        {
            if (seqEntry is not ValueDataNode value)
                throw new InvalidOperationException("ComponentFilter contained a non-value entry.");

            filter.Add(factory, value.Value);
        }

        return filter;
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        ComponentFilter value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var factory = dependencies.Resolve<IComponentFactory>();
        var seq = new SequenceDataNode();

        foreach (var componentType in value)
        {
            seq.Add(new ValueDataNode(factory.GetComponentName(componentType)));
        }

        return seq;
    }

    public SequenceDataNode PushInheritance(
        ISerializationManager serializationManager,
        SequenceDataNode child,
        SequenceDataNode parent,
        IDependencyCollection dependencies,
        ISerializationContext? context)
    {
        var setLeft = child.Select(x => (x as ValueDataNode)?.Value);
        var setRight = child.Select(x => (x as ValueDataNode)?.Value);

        var newSet = setLeft.Concat(setRight).ToHashSet();

        if (newSet.Contains(null))
            throw new InvalidOperationException("Pushing inheritance for filter failed due to non-value entries");

        return new SequenceDataNode(newSet.Select(x => new ValueDataNode(x!)).ToArray<DataNode>());
    }

    public void CopyTo(
        ISerializationManager serializationManager,
        ComponentFilter source,
        ref ComponentFilter target,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        target.Clear();
        target.UnionWith(source);
    }
}
