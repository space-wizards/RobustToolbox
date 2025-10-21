using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.Utility;

[Serializable, NetSerializable]
public sealed class MarkupNode : IComparable<MarkupNode>, IEquatable<MarkupNode>
{
    public readonly string? Name;
    public readonly MarkupParameter Value;
    public readonly Dictionary<string, MarkupParameter> Attributes;
    public readonly bool Closing;

    /// <summary>
    /// Creates a nameless tag for plaintext
    /// </summary>
    /// <param name="text">The plaintext the tag will consist of</param>
    public MarkupNode(string text)
    {
        Attributes = new Dictionary<string, MarkupParameter>();
        Value = new MarkupParameter(text);
    }

    public MarkupNode(string? name, MarkupParameter? value, Dictionary<string, MarkupParameter>? attributes, bool closing = false)
    {
        Name = name;
        Value = value ?? new MarkupParameter();
        Attributes = attributes ?? new Dictionary<string, MarkupParameter>();
        Closing = closing;
    }

    public override string ToString()
    {
        if(Name == null)
            return Value.StringValue ?? "";

        var attributesString = "";
        foreach (var (k, v) in Attributes)
        {
            attributesString += $"{k}{v}";
        }

        return $"[{(Closing ? "/" : "")}{Name}{Value.ToString().ReplaceLineEndings("\\n")}{attributesString}]";
    }

    public override bool Equals(object? obj)
    {
        return obj is MarkupNode node && Equals(node);
    }

    public bool Equals(MarkupNode? node)
    {
        if (node is null)
            return false;

        if (Name != node.Name)
            return false;

        if (!Value.Equals(node.Value))
            return false;

        if (Closing != node.Closing)
            return false;

        if (Attributes.Count != node.Attributes.Count)
            return false;

        foreach (var (key, value) in Attributes)
        {
            if (!node.Attributes.TryGetValue(key, out var nodeValue))
                return false;

            if (!value.Equals(nodeValue))
                return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Value, Closing);
    }

    public int CompareTo(MarkupNode? other)
    {
        if (ReferenceEquals(this, other))
            return 0;
        if (ReferenceEquals(null, other))
            return 1;

        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0)
            return nameComparison;

        return Closing.CompareTo(other.Closing);
    }
}

[Serializable, NetSerializable]
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

        if (ColorValue?.Name() != null)
            return ColorValue.Value.Name()!.Insert(0, "=");

        return ColorValue?.ToHex().Insert(0, "=") ?? "";
    }

    public bool Equals(MarkupParameter? other)
    {
        if (!other.HasValue)
            return false;

        var equal = StringValue == other.Value.StringValue;
        equal &= LongValue == other.Value.LongValue;
        equal &= ColorValue == other.Value.ColorValue;

        return equal;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(StringValue, LongValue, ColorValue);
    }
}
