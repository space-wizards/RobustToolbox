using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Localization;

public sealed class LocalizedStringListSerializer : ITypeSerializer<List<string>, SequenceDataNode>
{
    private LocalizedStringSerializer LocalizedSerializer => new();

    public ValidationNode Validate(ISerializationManager serializationManager, SequenceDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var list = new List<ValidationNode>();

        foreach (var dataNode in node.Sequence)
        {
            if (dataNode is not ValueDataNode value)
            {
                list.Add(new ErrorNode(dataNode, $"Cannot cast node {dataNode} to ValueDataNode."));
                continue;
            }

            list.Add(LocalizedSerializer.Validate(serializationManager, value, dependencies, context));
        }

        return new ValidatedSequenceNode(list);
    }

    public DataNode Write(ISerializationManager serializationManager, List<string> value, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var list = new List<DataNode>();

        foreach (var str in value)
        {
            list.Add(LocalizedSerializer.Write(serializationManager, str, alwaysWrite, context));
        }

        return new SequenceDataNode(list);
    }

    public List<string> Read(ISerializationManager serializationManager, SequenceDataNode node, IDependencyCollection dependencies,
        bool skipHook, ISerializationContext? context = null, List<string>? value = default)
    {
        value ??= new List<string>();

        foreach (var dataNode in node.Sequence)
        {
            value.Add(LocalizedSerializer.Read(
                serializationManager,
                (ValueDataNode) dataNode,
                dependencies,
                skipHook,
                context));
        }

        return value;
    }

    public List<string> Copy(ISerializationManager serializationManager, List<string> source, List<string> target, bool skipHook,
        ISerializationContext? context = null)
    {
        return new(source);
    }
}
