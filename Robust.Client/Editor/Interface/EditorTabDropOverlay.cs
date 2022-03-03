using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

namespace Robust.Client.Editor.Interface;

internal sealed class EditorTabDropOverlay : Control
{
    public float MarginSize { get; set; }

    public EditorTabDropOverlay()
    {
        MouseFilter = MouseFilterMode.Pass;
    }

    protected internal override void Draw(DrawingHandleScreen handle)
    {
        var (sizeX, sizeY) = PixelSize;

        var margin = MarginSize * UIScale;

        handle.DrawLine((0, 0), (sizeX, 0), Color.White);
        handle.DrawLine((sizeX, 0), (sizeX, sizeY), Color.White);
        handle.DrawLine((sizeX, sizeY), (0, sizeY), Color.White);
        handle.DrawLine((0, sizeY), (0, 0), Color.White);

        handle.DrawLine((margin, margin), (sizeX - margin, margin), Color.White);
        handle.DrawLine((sizeX - margin, margin), (sizeX - margin, sizeY - margin), Color.White);
        handle.DrawLine((sizeX - margin, sizeY - margin), (margin, sizeY - margin), Color.White);
        handle.DrawLine((margin, sizeY - margin), (margin, margin), Color.White);

        handle.DrawLine((0, 0), (margin, margin), Color.White);
        handle.DrawLine((sizeX, 0), (sizeX - margin, margin), Color.White);
        handle.DrawLine((sizeX, sizeY), (sizeX - margin, sizeY - margin), Color.White);
        handle.DrawLine((0, sizeY), (margin, sizeY - margin), Color.White);
    }
}
