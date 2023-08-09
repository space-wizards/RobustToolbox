using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.RichText;

public sealed class MarkupDrawingContext
{
    public readonly Stack<Color> Color;
    public readonly Stack<Font> Font;

    public MarkupDrawingContext()
    {
        Color = new Stack<Color>();
        Font = new Stack<Font>();
    }

    public MarkupDrawingContext(int capacity)
    {
        Color = new Stack<Color>(capacity);
        Font = new Stack<Font>(capacity);
    }

    public void Clear()
    {
        Color.Clear();
        Font.Clear();
    }
}
