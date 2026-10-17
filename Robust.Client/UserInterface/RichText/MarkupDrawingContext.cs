using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.RichText;

public sealed class MarkupDrawingContext(Control? owner, int capacity)
{
    public readonly Stack<Color> Color = new(capacity);
    public readonly Stack<Font> Font = new(capacity);
    public readonly List<IMarkupTagHandler> Tags = [];

    public MarkupDrawingContext() : this(null)
    {
    }

    public MarkupDrawingContext(int capacity) : this(null, capacity)
    {
    }

    public MarkupDrawingContext(Control? owner) : this(owner, 0)
    {
    }

    public void Clear()
    {
        Color.Clear();
        Font.Clear();
        Tags.Clear();
    }

    [PublicAPI]
    public bool TryGetStyleProperty<T>(string param, [MaybeNullWhen(false)] out T value)
    {
        value = default;
        return owner?.TryGetStyleProperty(param, out value) ?? false;
    }
}
