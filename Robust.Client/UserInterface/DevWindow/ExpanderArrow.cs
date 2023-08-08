using System;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface;

internal sealed class ExpanderArrow : Control
{
    public bool Rotated { get; set; }

    public Color Color = Color.White;

    public Color? OutlineColor;

    protected internal override void Draw(DrawingHandleScreen handle)
    {
        Span<Vector2> triangle = stackalloc Vector2[3];
        triangle[0] = new Vector2(0.8f, 0f);
        triangle[1] = new Vector2(-0.8f, 1f);
        triangle[2] = new Vector2(-0.8f, -1f);

        foreach (ref var t in triangle)
        {
            if (Rotated)
            {
                t = Angle.FromDegrees(90).RotateVec(t);
            }

            t /= 2;
            t += new Vector2(0.5f, 0.5f);
            t *= PixelSize;
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, triangle, Color);
        if (OutlineColor != null)
            handle.DrawPrimitives(DrawPrimitiveTopology.LineLoop, triangle, OutlineColor.Value);
    }
}
