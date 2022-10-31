using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.Shared.Serialization.Manager;

public sealed partial class SerializationManager
{
    //null values are the bane of my existence

    private T? GetNullable<T>() where T : struct
    {
        return null;
    }

    private T GetValueOrDefault<T>(object? value) where T : struct
    {
        return value as T? ?? default;
    }

    private bool IsNull(DataNode node)
    {
        return node is ValueDataNode valueDataNode && valueDataNode.Value.Trim().ToLower() is "null" or "";
    }
}
