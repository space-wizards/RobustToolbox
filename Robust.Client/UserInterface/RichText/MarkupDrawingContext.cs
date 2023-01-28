using System.Collections.Generic;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.RichText;

public sealed class MarkupDrawingContext
{
    public readonly Stack<Color> Color = new();
    public readonly Stack<Font> Font = new();
}
