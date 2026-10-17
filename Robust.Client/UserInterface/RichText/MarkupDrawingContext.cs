using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.RichText;

/// <summary>
/// The context used when drawing rich text markup.
/// </summary>
/// <param name="owner">The parent Control (for style properties), or null if none</param>
/// <param name="capacity">Initial capacity for the context stacks</param>
public sealed class MarkupDrawingContext(Control? owner, int capacity)
{
    /// <summary>
    /// The stack of font colors.
    /// </summary>
    public readonly Stack<Color> Color = new(capacity);

    /// <summary>
    /// The stack of fonts.
    /// </summary>
    public readonly Stack<Font> Font = new(capacity);

    /// <summary>
    /// Tags processed so far.
    /// </summary>
    /// <remarks>
    /// This is not currently populated or written to by anything.
    /// </remarks>
    public readonly List<IMarkupTagHandler> Tags = [];

    /// <summary>
    /// A markup drawing context without a parent control.
    /// </summary>
    [Obsolete("Use MarkupDrawingContext(Control owner) instead so tags can use style properties")]
    public MarkupDrawingContext() : this(null)
    {
    }

    /// <summary>
    /// The context used when drawing rich text markup.
    /// </summary>
    /// <param name="capacity">Initial capacity for the context stacks</param>
    [Obsolete("Use MarkupDrawingContext(Control owner, int capacity) instead so tags can use style properties")]
    public MarkupDrawingContext(int capacity) : this(null, capacity)
    {
    }

    /// <summary>
    /// The context used when drawing rich text markup.
    /// </summary>
    /// <param name="owner">The parent Control (for style properties), or null if none</param>
    public MarkupDrawingContext(Control? owner) : this(owner, 0)
    {
    }

    /// <summary>
    /// Clears the context.
    /// </summary>
    /// <remarks>
    /// This does not clear <see cref="owner"/>.
    /// </remarks>
    public void Clear()
    {
        Color.Clear();
        Font.Clear();
        Tags.Clear();
    }

    /// <summary>
    /// Tries to get the provided style property from the <see cref="owner"/>.
    /// </summary>
    /// <param name="param">Style property key</param>
    /// <param name="value">Style property value</param>
    /// <typeparam name="T">Type of the style property value</typeparam>
    /// <returns>True if found with type <see cref="T"/>, false if not (or false if there is no <see cref="_owner"/>)</returns>
    [PublicAPI]
    public bool TryGetStyleProperty<T>(string param, [MaybeNullWhen(false)] out T value)
    {
        value = default;
        return owner?.TryGetStyleProperty(param, out value) ?? false;
    }
}
