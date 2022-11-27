using System;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.Shared.Serialization.Markdown.Validation;

public sealed class FieldNotFoundErrorNode : ErrorNode
{
    public FieldNotFoundErrorNode(ValueDataNode key, Type type) : base(key, $"Field \"{key.Value}\" not found in \"{type}\".", false)
    {
    }
}
