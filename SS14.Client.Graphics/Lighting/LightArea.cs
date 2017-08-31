using OpenTK;
using SFML.Graphics;
using SS14.Client.Graphics.Render;

namespace SS14.Client.Graphics.Lighting
{
    public class LightArea : ILightArea
    {
        public LightArea(ShadowmapSize shadowmapSize, SFML.Graphics.Sprite mask)
        {
            var baseSize = 2 << (int) shadowmapSize;
            LightAreaSize = new Vector2(baseSize, baseSize);
            RenderTarget = new RenderImage("LightArea" + shadowmapSize, (uint) baseSize, (uint) baseSize);
            Mask = mask;
        }

        public RenderImage RenderTarget { get; }
        public SFML.Graphics.Sprite Mask { get; set; }
        public bool MaskFlipX { get; set; }
        public bool MaskFlipY { get; set; }
        public bool Rot90 { get; set; }

        public Vector4 MaskProps
        {
            get
            {
                if (Rot90 && MaskFlipX && MaskFlipY)
                    return maskPropsVec(false, false, false);
                if (Rot90 && MaskFlipX && !MaskFlipY)
                    return maskPropsVec(true, false, true);
                if (Rot90 && !MaskFlipX && MaskFlipY)
                    return maskPropsVec(true, true, false);
                if (Rot90 && !MaskFlipX && !MaskFlipY)
                    return maskPropsVec(true, false, false);
                if (!Rot90 && MaskFlipX && MaskFlipY)
                    return maskPropsVec(false, true, true);
                if (!Rot90 && MaskFlipX && !MaskFlipY)
                    return maskPropsVec(false, true, false);
                if (!Rot90 && !MaskFlipX && MaskFlipY)
                    return maskPropsVec(false, false, true);
                return maskPropsVec(false, false, false);
            }
        }

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

        public void SetMask(SFML.Graphics.Sprite mask)
        {
            Mask = mask;
        }

        private Vector4 maskPropsVec(bool rot, bool flipx, bool flipy)
        {
            return new Vector4(rot ? 1 : 0, flipx ? 1 : 0, flipy ? 1 : 0, 0);
        }
    }
}
