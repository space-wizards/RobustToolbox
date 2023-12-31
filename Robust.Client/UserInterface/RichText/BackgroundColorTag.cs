using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

/// <summary>
/// Colors the text inside its opening and closing nodes
/// </summary>
public sealed class BackgroundColorTag : IMarkupTag
{
    public static readonly Color? DefaultColor = null;

    public string Name => "bgcolor";

    /// <inheritdoc/>
    public void PushDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        if (node.Value.TryGetColor(out var color))
        {
            context.BackgroundColor.Push(color.Value);
            return;
        }

        context.BackgroundColor.Push(DefaultColor);
    }

    /// <inheritdoc/>
    public void PopDrawContext(MarkupNode node, MarkupDrawingContext context)
    {
        context.BackgroundColor.Pop();
    }
}
