using System.Collections.Generic;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        private readonly List<Viewport> _viewports = new List<Viewport>();

        private Viewport CreateViewport(Vector2i size, string? name = null)
        {
            var viewport = new Viewport(name, this);
            viewport.Size = size;
            viewport.RenderTarget = CreateRenderTarget(size,
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                name: $"{name}-MainRenderTarget");

            RegenLightRts(viewport);

            _viewports.Add(viewport);

            return viewport;
        }

        IClydeViewport IClyde.CreateViewport(Vector2i size, string? name)
        {
            return CreateViewport(size, name);
        }

        private static Vector2 ScreenToMap(Vector2 point, Viewport vp)
        {
            if (vp.Eye == null)
            {
                return Vector2.Zero;
            }

            // (inlined version of UiProjMatrix^-1)
            point -= vp.Size / 2f;
            point *= new Vector2(1, -1) / EyeManager.PixelsPerMeter;

            // view matrix
            vp.Eye.GetViewMatrixInv(out var viewMatrixInv);
            point = viewMatrixInv * point;

            return point;
        }

        private sealed class Viewport : IClydeViewport
        {
            private readonly Clyde _clyde;

            // Primary render target.
            public RenderTexture RenderTarget = default!;

            // Various render targets used in the light rendering process.

            // Lighting is drawn into this. This then gets sampled later while rendering world-space stuff.
            public RenderTexture LightRenderTarget = default!;

            // Unused, to be removed.
            public RenderTexture WallMaskRenderTarget = default!;

            // Two render targets used to apply gaussian blur to the _lightRenderTarget so it bleeds "into" walls.
            // We need two of them because efficient blur works in two stages and also we're doing multiple iterations.
            public RenderTexture WallBleedIntermediateRenderTarget1 = default!;
            public RenderTexture WallBleedIntermediateRenderTarget2 = default!;

            public string? Name { get; }

            public Viewport(string? name, Clyde clyde)
            {
                Name = name;
                _clyde = clyde;
            }

            public Vector2i Size { get; set; }

            void IClydeViewport.Render()
            {
                _clyde.RenderViewport(this);
            }

            public void Dispose()
            {
            }


            IRenderTexture IClydeViewport.RenderTarget => RenderTarget;
            public IEye? Eye { get; set; }

            /*public void Resize(Vector2i newSize)
            {
                Size = newSize;
            }*/
        }
    }
}
