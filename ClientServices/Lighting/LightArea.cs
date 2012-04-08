using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace SS3D.LightTest
{
    class LightArea
    {
        public RenderImage renderTarget { get; private set; }
        public Vector2D LightPosition { get; set; }
        public Vector2D LightAreaSize { get; set; }

        public LightArea(ShadowmapSize size)
        {
            int baseSize = 2 << (int)size;
            LightAreaSize = new Vector2D(baseSize, baseSize);
            renderTarget = new RenderImage("lightTest", baseSize, baseSize, ImageBufferFormats.BufferRGB888A8);

        }

        public Vector2D ToRelativePosition(Vector2D worldPosition)
        {
            return worldPosition - (LightPosition - LightAreaSize * 0.5f);
        }

        public void BeginDrawingShadowCasters()
        {
            Gorgon.CurrentRenderTarget = renderTarget;
            Gorgon.CurrentRenderTarget.Clear(System.Drawing.Color.FromArgb(0, 0, 0, 0));
        }

        public void EndDrawingShadowCasters()
        {
            Gorgon.CurrentRenderTarget = null;
        }

    }
}
