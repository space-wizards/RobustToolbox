using System;
using System.Collections.Generic;
using Robust.Client.Graphics.ClientEye;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.ClientEye;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Clyde
{
    internal sealed partial class Clyde
    {
        // Use WeakReference here instead of a separate Loaded* object since these only contain managed objects.
        private readonly Dictionary<ClydeHandle, WeakReference<Viewport>> _viewports =
            new();

        private Viewport CreateViewport(Vector2i size, string? name = null)
        {
            var handle = AllocRid();
            var viewport = new Viewport(handle, name, this)
            {
                Size = size,
                RenderTarget = CreateRenderTarget(size,
                    new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                    name: $"{name}-MainRenderTarget")
            };

            RegenLightRts(viewport);

            _viewports.Add(handle, new WeakReference<Viewport>(viewport));

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

        private void FlushViewportDispose()
        {
            // Free of allocations unless a dead viewport is found.
            List<ClydeHandle>? toRemove = null;
            foreach (var (handle, viewportRef) in _viewports)
            {
                if (!viewportRef.TryGetTarget(out _))
                {
                    toRemove ??= new List<ClydeHandle>();
                    toRemove.Add(handle);
                }
            }

            if (toRemove == null)
            {
                return;
            }

            foreach (var remove in toRemove)
            {
                _viewports.Remove(remove);
            }
        }

        private sealed class Viewport : IClydeViewport
        {
            private readonly ClydeHandle _handle;
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

            public Viewport(ClydeHandle handle, string? name, Clyde clyde)
            {
                Name = name;
                _handle = handle;
                _clyde = clyde;
            }

            public Vector2i Size { get; set; }

            void IClydeViewport.Render()
            {
                _clyde.RenderViewport(this);
            }

            public Vector2 WorldToLocal(Vector2 point)
            {
                if (Eye == null)
                    return (0, 0);

                var eye = (IEye) Eye;
                var newPoint = point;

                eye.GetViewMatrix(out var viewMatrix);
                newPoint = viewMatrix * newPoint;

                // (inlined version of UiProjMatrix)
                newPoint *= new Vector2(1, -1) * EyeManager.PixelsPerMeter;
                newPoint += Size / 2f;

                return newPoint;
            }

            public void Dispose()
            {
                RenderTarget.Dispose();
                LightRenderTarget.Dispose();
                WallMaskRenderTarget.Dispose();
                WallBleedIntermediateRenderTarget1.Dispose();
                WallBleedIntermediateRenderTarget2.Dispose();

                _clyde._viewports.Remove(_handle);
            }

            IRenderTexture IClydeViewport.RenderTarget => RenderTarget;
            public IEye? Eye { get; set; }
        }
    }
}
