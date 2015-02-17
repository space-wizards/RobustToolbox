using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFML.Graphics;
using Color = SFML.Graphics.Color;

namespace SS14.Client.Graphics.CluwneLib.Render
{
    public class RenderImage : RenderTexture
    {
        public RenderImage(uint width, uint height) : base(width, height)
        {
        }

        public RenderImage(uint width, uint height, bool depthBuffer) : base(width, height, depthBuffer)
        {
        }

        public void Blit(float x, float y, float width, float height, Color color, RenderStates state)
        {
            
        }
    }
}
