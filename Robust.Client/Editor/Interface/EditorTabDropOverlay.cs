using System.Numerics;
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

        handle.DrawLine(new Vector2(0, 0), new Vector2(sizeX, 0), Color.White);
        handle.DrawLine(new Vector2(sizeX, 0), new Vector2(sizeX, sizeY), Color.White);
        handle.DrawLine(new Vector2(sizeX, sizeY), new Vector2(0, sizeY), Color.White);
        handle.DrawLine(new Vector2(0, sizeY), new Vector2(0, 0), Color.White);

        handle.DrawLine(new Vector2(margin, margin), new Vector2(sizeX - margin, margin), Color.White);
        handle.DrawLine(new Vector2(sizeX - margin, margin), new Vector2(sizeX - margin, sizeY - margin), Color.White);
        handle.DrawLine(new Vector2(sizeX - margin, sizeY - margin), new Vector2(margin, sizeY - margin), Color.White);
        handle.DrawLine(new Vector2(margin, sizeY - margin), new Vector2(margin, margin), Color.White);

        handle.DrawLine(new Vector2(0, 0), new Vector2(margin, margin), Color.White);
        handle.DrawLine(new Vector2(sizeX, 0), new Vector2(sizeX - margin, margin), Color.White);
        handle.DrawLine(new Vector2(sizeX, sizeY), new Vector2(sizeX - margin, sizeY - margin), Color.White);
        handle.DrawLine(new Vector2(0, sizeY), new Vector2(margin, sizeY - margin), Color.White);
    }
}
