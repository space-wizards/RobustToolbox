using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace SS14.Client.Interfaces.Lighting
{
    public interface ILightArea
    {
        RenderImage renderTarget { get; }
        Vector2D LightPosition { get; set; }
        Vector2D LightAreaSize { get; set; }
        bool Calculated { get; set; }
        Sprite Mask { get; set; }
        bool MaskFlipX { get; set; }
        bool MaskFlipY { get; set; }
        bool Rot90 { get; set; }
        Vector4D MaskProps { get; }
        Vector2D ToRelativePosition(Vector2D worldPosition);
        void BeginDrawingShadowCasters();
        void EndDrawingShadowCasters();

        void SetMask(string mask);
    }
}