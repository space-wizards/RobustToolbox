using System.Collections.Generic;
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Maths;

namespace Robust.Client.MapEditor;

internal sealed class GridBackgroundOverlay : Overlay
{
    public readonly Dictionary<long, Parameters> ViewportParameters = new();

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowWorld;

    protected internal override void Draw(in OverlayDrawArgs args)
    {
        if (!ViewportParameters.TryGetValue(args.Viewport.Id, out var parameters))
            return;

        // TODO: This sucks and should just cover the entire WorldAABB.
        // Also, line thickness probably needs accounting for. Probably needs to become a screen-space overlay?
        for (var x = -128; x < 128; x += 8)
        {
            var color = parameters.Color;
            if (x == 0)
                color = parameters.YAxisColor;

            args.WorldHandle.DrawLine(new Vector2(x, -128), new Vector2(x, 128), color);
        }

        for (var y = -128; y < 128; y += 8)
        {
            var color = parameters.Color;
            if (y == 0)
                color = parameters.XAxisColor;

            args.WorldHandle.DrawLine(new Vector2(-128, y), new Vector2(128, y), color);
        }
    }

    public sealed class Parameters
    {
        public Color Color = Color.FromHex("#222");
        public Color XAxisColor = Color.FromHex("#A22");
        public Color YAxisColor = Color.FromHex("#2A2");
    }
}
