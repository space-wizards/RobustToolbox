using System;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.RichText;

/// <summary>
///     Contains a simple string of text formatted with markup tags.
/// </summary>
/// <remarks>
/// <para>
///     This type differs from <see cref="FormattedMessage"/> by storing purely a markup string,
///     rather than a full parsed object model.
///     This makes it significantly more lightweight than <see cref="FormattedMessage"/>,
///     and suitable for places where markup only has to be <i>passed around</i>, rather than modified or interpreted.
/// </para>
/// </remarks>
public struct FormattedString : IEquatable<FormattedString>, ISelfSerialize
{
    // NOTE: This type has a custom network type serializer.

    /// <summary>
    /// Represents an empty (<see cref="string.Empty"/>) string.
    /// </summary>
    public static readonly FormattedString Empty = new("");

    /// <summary>
    ///     The contained markup text.
    /// </summary>
    /// <remarks>
    ///     This must always be strict valid markup, i.e. parseable by <see cref="FormattedMessage.ParseOrThrow"/>.
    /// </remarks>
    public readonly string Markup;

    [Obsolete("Do not construct FormattedString directly")]
    public FormattedString()
    {
        throw new NotSupportedException("Do not construct FormattedString directly");
    }

    /// <summary>
    /// Internal constructor, does not validate markup is strictly valid.
    /// </summary>
    /// <param name="markup"></param>
    private FormattedString(string markup)
    {
        Markup = markup;
    }

    /// <summary>
    ///     Create a <see cref="FormattedString"/> from a strict markup string.
    /// </summary>
    /// <remarks>
    ///     The provided markup string must be strict valid markup,
    ///     i.e. parseable by <see cref="FormattedMessage.ParseOrThrow"/>.
    /// </remarks>
    /// <exception cref="ArgumentException">
    ///     Thrown of <paramref name="markup"/> is not strict valid markup.
    /// </exception>
    /// <seealso cref="FromMarkupPermissive"/>
    public static FormattedString FromMarkup(string markup)
    {
        if (!FormattedMessage.ValidMarkup(markup))
            throw new ArgumentException("Invalid markup string");

        return new FormattedString(markup);
    }

    /// <summary>
    ///     Create a <see cref="FormattedString"/> from a permissive markup string.
    /// </summary>
    /// <remarks>
    ///     The provided markup string does not need to be strict valid markup,
    ///     but it will be normalized to be strict if it's not.
    /// </remarks>
    /// <seealso cref="FromMarkup"/>
    public static FormattedString FromMarkupPermissive(string markup)
    {
        // We round trip here to ensure the contents are valid.
        var permissive = FormattedMessage.FromMarkupPermissive(markup);
        return (FormattedString)permissive;
    }

    /// <summary>
    ///     Create a <see cref="FormattedString"/> from plaintext (escaping it if necessary).
    /// </summary>
    /// <remarks>
    ///     This is equivalent to <see cref="FormattedMessage.EscapeText"/>
    /// </remarks>
    public static FormattedString FromPlainText(string plainText)
    {
        return new FormattedString(FormattedMessage.EscapeText(plainText));
    }

    public static explicit operator FormattedString(FormattedMessage message)
    {
        // Assumed to be valid markup returned by ToMarkup().
        return new FormattedString(message.ToMarkup());
    }

    public static explicit operator FormattedMessage(FormattedString str)
    {
        // This should never throw.
        return FormattedMessage.FromMarkupOrThrow(str.Markup);
    }

    public static explicit operator string(FormattedString str)
    {
        return str.Markup;
    }

    public readonly bool Equals(FormattedString other)
    {
        return other.Markup == Markup;
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is FormattedString other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        return Markup.GetHashCode();
    }

    public static bool operator ==(FormattedString left, FormattedString right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FormattedString left, FormattedString right)
    {
        return !left.Equals(right);
    }

    void ISelfSerialize.Deserialize(string value)
    {
        this = FromMarkup(value);
    }

    readonly string ISelfSerialize.Serialize()
    {
        return Markup;
    }
}
