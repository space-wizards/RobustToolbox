using System;
using System.Text;
using Robust.Shared.Collections;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.RichText;

/// <summary>
/// A wrapper around <see cref="StringBuilder"/>, with convenience methods for safely constructing rich text markup.
/// </summary>
/// <remarks>
/// <para>
/// Tags are written with multiple consecutive calls. Functions may throw if not in the right state,
/// and this can be checked with <see cref="IsInsideTag"/>.
/// It should go without saying that care must be taken to use the underlying <see cref="StringBuilder"/>
/// while this is the case.
/// </para>
/// <para>
/// While the underlying <see cref="StringBuilder"/> is accessible, you are of course responsible for writing valid
/// markup and escaping if necessary.
/// </para>
/// </remarks>
public sealed class FormattedStringBuilder
{
    private ValueList<string> _tagStack;

    /// <summary>
    /// The underlying <see cref="StringBuilder"/> used by this instance.
    /// </summary>
    /// <remarks>
    /// you are responsible for writing valid markup and escaping where necessary, if you access this property.
    /// </remarks>
    public StringBuilder Builder { get; }

    /// <summary>
    /// If true, we are currently writing a tag.
    /// </summary>
    /// <remarks>
    /// This can be ended through <see cref="FinishTagSelfClosed"/> or <see cref="FinishTagOpen"/>.
    /// </remarks>
    public bool IsInsideTag { get; private set; } = true;

    /// <summary>
    /// Create a new builder with an empty underlying <see cref="StringBuilder"/>.
    /// </summary>
    public FormattedStringBuilder() : this(new StringBuilder())
    {

    }

    /// <summary>
    /// Create a new builder wrapping an existing <see cref="StringBuilder"/>.
    /// </summary>
    /// <remarks>
    /// The provided instance is not initially mutated.
    /// </remarks>
    public FormattedStringBuilder(StringBuilder builder)
    {
        Builder = builder;
    }

    /// <summary>
    /// Begin a new tag with the specified name.
    /// </summary>
    /// <param name="tagName">The name of the tag to begin.</param>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're already inside a tag.
    /// </exception>
    public FormattedStringBuilder BeginTag(string tagName)
    {
        if (!IsInsideTag)
            throw new InvalidOperationException("Cannot begin tag: we're already in a tag");

        _tagStack.Push(tagName);
        IsInsideTag = false;
        Builder.Append($"[{tagName}");

        return this;
    }

    /// <summary>
    /// Begin a new tag with the specified name and a value.
    /// </summary>
    /// <param name="tagName">The name of the tag to begin.</param>
    /// <param name="value">The value of the markup tag.</param>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're already inside a tag.
    /// </exception>
    public FormattedStringBuilder BeginTag(string tagName, MarkupParameter value)
    {
        BeginTag(tagName);

        Builder.Append(value.ToString());

        return this;
    }

    /// <summary>
    /// Begin a new tag with the specified name and a value.
    /// </summary>
    /// <param name="tagName">The name of the tag to begin.</param>
    /// <param name="value">The value of the markup tag.</param>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're already inside a tag.
    /// </exception>
    public FormattedStringBuilder BeginTag(string tagName, string value)
    {
        return BeginTag(tagName, new MarkupParameter(value));
    }

    /// <summary>
    /// Begin a new tag with the specified name and a value.
    /// </summary>
    /// <param name="tagName">The name of the tag to begin.</param>
    /// <param name="value">The value of the markup tag.</param>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're already inside a tag.
    /// </exception>
    public FormattedStringBuilder BeginTag(string tagName, long value)
    {
        return BeginTag(tagName, new MarkupParameter(value));
    }

    /// <summary>
    /// Begin a new tag with the specified name and a value.
    /// </summary>
    /// <param name="tagName">The name of the tag to begin.</param>
    /// <param name="value">The value of the markup tag.</param>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're already inside a tag.
    /// </exception>
    public FormattedStringBuilder BeginTag(string tagName, Color value)
    {
        return BeginTag(tagName, new MarkupParameter(value));
    }

    /// <summary>
    /// Specify an attribute for the tag currently being written.
    /// </summary>
    /// <remarks>
    /// This does not check for duplicates.
    /// </remarks>
    /// <param name="attributeName">The name of the attribute to write.</param>
    /// <param name="value">The value of the attribute.</param>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're not currently inside a tag.
    /// </exception>
    public FormattedStringBuilder TagAttribute(string attributeName, MarkupParameter value)
    {
        if (IsInsideTag)
            throw new InvalidOperationException("Cannot write attribute: we aren't in a tag!");

        Builder.Append($" {attributeName}{value}");
        return this;
    }

    /// <summary>
    /// Specify an attribute for the tag currently being written.
    /// </summary>
    /// <remarks>
    /// This does not check for duplicates.
    /// </remarks>
    /// <param name="attributeName">The name of the attribute to write.</param>
    /// <param name="value">The value of the attribute.</param>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're not currently inside a tag.
    /// </exception>
    public FormattedStringBuilder TagAttribute(string attributeName, string value)
    {
        return TagAttribute(attributeName, new MarkupParameter(value));
    }

    /// <summary>
    /// Specify an attribute for the tag currently being written.
    /// </summary>
    /// <remarks>
    /// This does not check for duplicates.
    /// </remarks>
    /// <param name="attributeName">The name of the attribute to write.</param>
    /// <param name="value">The value of the attribute.</param>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're not currently inside a tag.
    /// </exception>
    public FormattedStringBuilder TagAttribute(string attributeName, long value)
    {
        return TagAttribute(attributeName, new MarkupParameter(value));
    }

    /// <summary>
    /// Specify an attribute for the tag currently being written.
    /// </summary>
    /// <remarks>
    /// This does not check for duplicates.
    /// </remarks>
    /// <param name="attributeName">The name of the attribute to write.</param>
    /// <param name="value">The value of the attribute.</param>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're not currently inside a tag.
    /// </exception>
    public FormattedStringBuilder TagAttribute(string attributeName, Color value)
    {
        return TagAttribute(attributeName, new MarkupParameter(value));
    }

    /// <summary>
    /// Finish writing the current tag as self-closed.
    /// </summary>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're not currently inside a tag.
    /// </exception>
    public FormattedStringBuilder FinishTagSelfClosed()
    {
        if (IsInsideTag)
            throw new InvalidOperationException("Cannot finish tag: we aren't in a tag!");

        _tagStack.Pop();
        Builder.Append("/]");
        IsInsideTag = true;
        return this;
    }

    /// <summary>
    /// Finish writing the current tag as open. You will have to close it with <see cref="PopTag"/>.
    /// </summary>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're not currently inside a tag.
    /// </exception>
    public FormattedStringBuilder FinishTagOpen()
    {
        if (IsInsideTag)
            throw new InvalidOperationException("Cannot finish tag: we aren't in a tag!");

        Builder.Append(']');
        IsInsideTag = true;
        return this;
    }

    /// <summary>
    /// Write a closing tag for the most recent open tag.
    /// </summary>
    /// <remarks>
    /// The stack of open tags (from <see cref="FinishTagOpen"/>) is automatically tracked.
    /// </remarks>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public FormattedStringBuilder PopTag()
    {
        if (!IsInsideTag)
            throw new InvalidOperationException("Cannot begin tag: we're already in a tag");

        Builder.Append($"[/{_tagStack.Pop()}]");
        return this;
    }

    /// <summary>
    /// Append plain text.
    /// </summary>
    /// <param name="text">The text to append without interpreting formatting.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're currently inside a tag.
    /// </exception>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    public FormattedStringBuilder AppendText(string text)
    {
        CheckSafe();
        Builder.Append(FormattedMessage.EscapeText(text));
        return this;
    }

    /// <summary>
    /// Append markup.
    /// </summary>
    /// <param name="markup">The text to append as markup.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="markup"/> is not valid markup.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're currently inside a tag.
    /// </exception>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    public FormattedStringBuilder AppendMarkup(string markup)
    {
        CheckSafe();

        if (!FormattedMessage.ValidMarkup(markup))
            throw new ArgumentException("Not valid markup!", nameof(markup));

        Builder.Append(markup);
        return this;
    }

    /// <summary>
    /// Append markup, followed by a newline.
    /// </summary>
    /// <remarks>
    /// The added line is always a single Line Feed (LF), not <see cref="Environment.NewLine"/>.
    /// </remarks>
    /// <param name="markup">The text to append as markup.</param>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="markup"/> is not valid markup.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're currently inside a tag.
    /// </exception>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    public FormattedStringBuilder AppendMarkupLine(string markup)
    {
        AppendMarkup(markup);
        AppendLine();
        return this;
    }

    /// <summary>
    /// Append a newline.
    /// </summary>
    /// <remarks>
    /// The added line is always a single Line Feed (LF), not <see cref="Environment.NewLine"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if we're currently inside a tag.
    /// </exception>
    /// <returns>The current instance, to enable easy method call chaining.</returns>
    public FormattedStringBuilder AppendLine()
    {
        CheckSafe();
        Builder.Append('\n');
        return this;
    }

    /// <summary>
    /// Returns the internal value as a string.
    /// </summary>
    public override string ToString()
    {
        return Builder.ToString();
    }

    /// <summary>
    /// Returns the internal value as a <see cref="FormattedString"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if the contained string is not valid markup
    /// (e.g. if you manually messed with the underlying <see cref="StringBuilder"/>.)
    /// </exception>
    public FormattedString ToFormattedString()
    {
        return FormattedString.FromMarkup(ToString());
    }

    private void CheckSafe()
    {
        if (!IsInsideTag)
            throw new InvalidOperationException("Cannot append: we are currently writing a tag.");
    }
}
