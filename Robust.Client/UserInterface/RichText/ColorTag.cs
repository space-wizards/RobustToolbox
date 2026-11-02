using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.RichText;

/// <summary>
/// Colors the text inside its opening and closing nodes
/// </summary>
public sealed class ColorTag : IMarkupTagHandler
{
    public const string StylePropertyColorPrefix = "color-";
    public const string StylePropertyDefaultColor = "font-color";

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

        if (node.Value.TryGetString(out var name) &&
            context.TryGetStyleProperty<Color>(StylePropertyColorPrefix + name, out var value))
        {
            context.Color.Push(value);
            return;
        }

        if (context.TryGetStyleProperty<Color>(StylePropertyDefaultColor, out var defaultColor))
        {
            context.Color.Push(defaultColor);
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
