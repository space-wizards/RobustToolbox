using System;

namespace Robust.Shared.Serialization.Manager.Definition;

public static class DataDefinitionUtility
{
    public static string AutoGenerateTag(string name)
    {
        var span = name.AsSpan();
        return $"{char.ToLowerInvariant(span[0])}{span.Slice(1).ToString()}";
    }
}
