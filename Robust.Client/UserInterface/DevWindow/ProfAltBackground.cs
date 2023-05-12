using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface;

internal sealed class ProfAltBackground : Control
{
    public bool IsAltBackground { get; set; }
    public Color Color = DefaultColor;
    public static readonly Color DefaultColor = Color.FromHex("#222222");

    protected override void StylePropertiesChanged()
    {
        base.StylePropertiesChanged();
        if (!TryGetStyleProperty("color", out Color))
            Color = DefaultColor;
    }

    protected internal override void Draw(DrawingHandleScreen handle)
    {
        if (IsAltBackground)
            handle.DrawRect(PixelSizeBox, Color);
    }
}
