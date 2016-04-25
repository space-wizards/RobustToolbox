using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.Render;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Lighting
{
    public interface ILightArea
    {
        RenderImage RenderTarget { get; }
        Vector2f LightPosition { get; set; }
        Vector2f LightAreaSize { get; set; }
        bool Calculated { get; set; }
        Sprite Mask { get; set; }
        bool MaskFlipX { get; set; }
        bool MaskFlipY { get; set; }
        bool Rot90 { get; set; }
        Vector4f MaskProps { get; }
        Vector2f ToRelativePosition(Vector2f worldPosition);
        void BeginDrawingShadowCasters();
        void EndDrawingShadowCasters();

        void SetMask(string mask);
    }
}