using System;
using Robust.Client.Graphics;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface;

internal sealed class ExpanderArrow : Control
{
    public bool Rotated { get; set; }

    protected internal override void Draw(DrawingHandleScreen handle)
    {
        Span<Vector2> triangle = stackalloc Vector2[3];
        triangle[0] = (0.8f, 0f);
        triangle[1] = (-0.8f, 1f);
        triangle[2] = (-0.8f, -1f);

        foreach (ref var t in triangle)
        {
            if (Rotated)
            {
                t = Angle.FromDegrees(90).RotateVec(t);
            }

            t /= 2;
            t += 0.5f;
            t *= PixelSize;
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, triangle, Color.White);
    }
}
