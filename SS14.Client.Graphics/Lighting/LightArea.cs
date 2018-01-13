using SS14.Client.Graphics.Sprites;
using SS14.Client.Graphics.Render;
using Vector2 = SS14.Shared.Maths.Vector2;
using Color = SS14.Shared.Maths.Color;

namespace SS14.Client.Graphics.Lighting
{
    public class LightArea : ILightArea
    {
        public LightArea(ShadowmapSize shadowmapSize, Sprite mask)
        {
            var baseSize = 2 << (int) shadowmapSize;
            LightAreaSize = new Vector2(baseSize, baseSize);
            RenderTarget = new RenderImage("LightArea" + shadowmapSize, (uint) baseSize, (uint) baseSize);
            Mask = mask;
        }

        public RenderImage RenderTarget { get; }
        public Sprite Mask { get; set; }

        /// <summary>
        ///     World position coordinates of the light's center
        /// </summary>
        public Vector2 LightPosition { get; set; }

        public Vector2 LightAreaSize { get; set; }
        public bool Calculated { get; set; }

        public Vector2 ToRelativePosition(Vector2 worldPosition)
        {
            return worldPosition - (CluwneLib.WorldToScreen(LightPosition) - LightAreaSize * 0.5f);
        }

        public void BeginDrawingShadowCasters()
        {
            RenderTarget.BeginDrawing();

            RenderTarget.Clear(new Color(0, 0, 0, 0));
        }

        public void EndDrawingShadowCasters()
        {
            RenderTarget.EndDrawing();
        }

        public void SetMask(Sprite mask)
        {
            Mask = mask;
        }
    }
}
