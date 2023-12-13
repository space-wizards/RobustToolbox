using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.RichText;

public sealed class MarkupDrawingContext
{
    public readonly Stack<Color> Color;
    public readonly Stack<Font> Font;
    public readonly List<IMarkupTag> Tags;

    public MarkupDrawingContext()
    {
        Color = new Stack<Color>();
        Font = new Stack<Font>();
        Tags = new List<IMarkupTag>();
    }

    public MarkupDrawingContext(int capacity)
    {
        Color = new Stack<Color>(capacity);
        Font = new Stack<Font>(capacity);
        Tags = new List<IMarkupTag>();
    }

    public void Clear()
    {
        Color.Clear();
        Font.Clear();
        Tags.Clear();
    }
}
