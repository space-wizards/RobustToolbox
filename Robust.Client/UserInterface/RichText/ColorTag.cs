using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

public sealed class ColorTag : IMarkupTag
{
    private static readonly Color DefaultColor = new(200, 200, 200);

    public string Name => "color";

    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        if (node.Value.TryGetColor(out var color))
        {
            context.Color.Push(color.Value);
            return;
        }

        context.Color.Push(DefaultColor);
    }

    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Color.Pop();
    }
}
