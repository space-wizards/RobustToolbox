using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.Graphics.ClientEye
{
    public interface IEyeManager
    {
        IEye CurrentEye { get; set; }
        void Initialize();

        Vector2 WorldToScreen(Vector2 point);
        ScreenCoordinates WorldToScreen(LocalCoordinates point);
        LocalCoordinates ScreenToWorld(ScreenCoordinates point);
        Vector2 ScreenToWorld(Vector2 point);
    }
}
