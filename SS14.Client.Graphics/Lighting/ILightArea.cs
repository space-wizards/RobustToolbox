using OpenTK;

namespace SS14.Client.Graphics.Lighting
{
    public interface ILightArea
    {
        Vector2 LightPosition { get; set; }
        Vector2 LightAreaSize { get; }
        bool Calculated { get; set; }
        Vector2 ToRelativePosition(Vector2 worldPosition);
        void BeginDrawingShadowCasters();
        void EndDrawingShadowCasters();

        void SetMask(SFML.Graphics.Sprite mask);
    }
}
