using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;

namespace Robust.Shared.Utility;

public sealed class MarkupNode
{
    public readonly string? Name;
    public readonly MarkupParameter? Value;
    public readonly Dictionary<string, MarkupParameter>? Attributes;
    public readonly bool Closing;

    /// <summary>
    /// Creates a nameless tag for plaintext
    /// </summary>
    /// <param name="text">The plaintext the tag will consist of</param>
    public MarkupNode(string text)
    {
        Value = new MarkupParameter(text);
    }

    public override string ToString()
    {
        if(Name == null)
            return Value?.StringValue ?? "";

        var attributesString = "";
        foreach (var (k, v) in Attributes ?? new Dictionary<string, MarkupParameter>())
        {
            attributesString += $" {k}{("=\"")}{v}{("\"")}";
        }

        return $"[{(Closing ? "/" : "")}{Name}{(Value != null ? "=\"" : "")}{Value?.ToString().ReplaceLineEndings("\\n") ?? ""}{(Value != null ? "\"" : "")}{attributesString}]";
    }

    public MarkupNode(string? name, MarkupParameter? value, Dictionary<string, MarkupParameter>? attributes, bool closing = false)
    {
        Name = name;
        Value = value;
        Attributes = attributes;
        Closing = closing;
    }
}

public readonly record struct MarkupParameter(string? StringValue = null, long? LongValue = null, Color? ColorValue = null)
{
    public MarkupParameter(string? stringValue) : this(StringValue: stringValue)
    {
    }

    public MarkupParameter(long? longValue) : this(LongValue: longValue)
    {
    }

    public MarkupParameter(Color? colorValue) : this(ColorValue: colorValue)
    {
    }

    public bool TryGetString([NotNullWhen(true)] out string? value)
    {
        value = StringValue;
        return StringValue != null;
    }

    public bool TryGetLong([NotNullWhen(true)] out long? value)
    {
        value = LongValue;
        return LongValue.HasValue;
    }

    public bool TryGetColor([NotNullWhen(true)] out Color? value)
    {
        value = ColorValue;
        return ColorValue.HasValue;
    }


    public override string ToString()
    {
        if (StringValue != null)
            return $"=\"{StringValue}\"";

        if (LongValue.HasValue)
            return LongValue?.ToString().Insert(0, "=") ?? "";

        return ColorValue?.ToString().Insert(0, "=") ?? "";
    }
}
