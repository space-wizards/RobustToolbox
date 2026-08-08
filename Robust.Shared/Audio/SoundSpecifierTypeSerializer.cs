using System;
using System.Diagnostics;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Audio;

[TypeSerializer]
public sealed class SoundSpecifierTypeSerializer :
    ITypeReader<SoundSpecifier, MappingDataNode>,
    ITypeReader<SoundSpecifier, ValueDataNode>
{
    private Type GetType(MappingDataNode node)
    {
        var hasPath = node.Has(SoundPathSpecifier.Node);
        var hasCollection = node.Has(SoundCollectionSpecifier.Node);

        if (hasPath || !(hasPath ^ hasCollection))
            return typeof(SoundPathSpecifier);

        if (hasCollection)
            return typeof(SoundCollectionSpecifier);

        // See Read below if you are adding new types
        return typeof(SoundPathSpecifier);
    }

    public SoundSpecifier Read(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<SoundSpecifier>? instanceProvider = null)
    {
        var type = GetType(node);

        if (type == typeof(SoundPathSpecifier))
            return serializationManager.Read<SoundPathSpecifier>(node, hookCtx, context, notNullableOverride: true);

        if (type == typeof(SoundCollectionSpecifier))
            return serializationManager.Read<SoundCollectionSpecifier>(node, hookCtx, context, notNullableOverride: true);

        // See GetType above if you are adding new types
        throw new NotImplementedException();
    }

    public SoundSpecifier Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<SoundSpecifier>? instanceProvider = null)
    {
        return new SoundPathSpecifier(node.Value);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        if (node.Has(SoundPathSpecifier.Node) && node.Has(SoundCollectionSpecifier.Node))
            return new ErrorNode(node, "You can only specify either a sound path or a sound collection!");

        if (!node.Has(SoundPathSpecifier.Node) && !node.Has(SoundCollectionSpecifier.Node))
            return new ErrorNode(node, "You need to specify either a sound path or a sound collection!");

        return serializationManager.ValidateNode(GetType(node), node, context);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        if (serializationManager.ValidateNode<ResPath>(node, context) is not ErrorNode)
            return new ValidatedValueNode(node);

        return new ErrorNode(node, "SoundSpecifier value is not a valid resource path!");
    }
}
