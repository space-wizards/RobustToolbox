using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Interfaces.Graphics.ClientEye
{
    public interface IEyeManager
    {
        IEye CurrentEye { get; set; }
        /// <summary>
        ///     The ID of the map on which the current eye is "placed".
        /// </summary>
        MapId CurrentMap { get; }

        /// <summary>
        ///     A world-space box that is at LEAST the area covered by the viewport.
        ///     May be larger due to say rotation.
        /// </summary>
        Box2 GetWorldViewport();
        void Initialize();

        Vector2 WorldToScreen(Vector2 point);
        ScreenCoordinates WorldToScreen(GridCoordinates point);
        GridCoordinates ScreenToWorld(ScreenCoordinates point);
        GridCoordinates ScreenToWorld(Vector2 point);
    }
}
