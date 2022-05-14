using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface;

internal sealed class ProfAltBackground : Control
{
    public bool IsAltBackground { get; set; }

    protected internal override void Draw(DrawingHandleScreen handle)
    {
        if (IsAltBackground)
            handle.DrawRect(PixelSizeBox, new Color(0, 0, 0, 128));
    }
}
