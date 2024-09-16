using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

/// <summary>
/// Colors the text inside its opening and closing nodes
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
public sealed class ColorTag : IMarkupTag
#pragma warning restore CS0618 // Type or member is obsolete
{
    public static readonly Color DefaultColor = new(200, 200, 200);

    public string Name => "color";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        if (node.Value.TryGetColor(out var color))
        {
            context.Color.Push(color.Value);
            return;
        }

        context.Color.Push(DefaultColor);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.Color.Pop();
    }
}
