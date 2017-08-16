using SFML.Graphics;
using SFML.System;
using SS14.Client.Graphics.Render;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Lighting
{
    public interface ILightArea
    {
        RenderImage RenderTarget { get; }
        Vector2 LightPosition { get; set; }
        Vector2 LightAreaSize { get; set; }
        bool Calculated { get; set; }
        Sprite Mask { get; set; }
        bool MaskFlipX { get; set; }
        bool MaskFlipY { get; set; }
        bool Rot90 { get; set; }
        Vector4 MaskProps { get; }
        Vector2 ToRelativePosition(Vector2 worldPosition);
        void BeginDrawingShadowCasters();
        void EndDrawingShadowCasters();

        void SetMask(string mask);
    }
}