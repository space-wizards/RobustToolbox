using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface.CustomControls
{
    /// <summary>
    ///     Base interface for controls that display a viewport.
    /// </summary>
    /// <remarks>
    ///     This has to be implemented for correct handling of input,
    ///     you do not strictly need to implement this otherwise.
    /// </remarks>
    public interface IViewportControl
    {
        IClydeViewport? Viewport { get; set; }

        MapCoordinates ScreenToMap(Vector2 coords);
        Vector2 WorldToScreen(Vector2 map);
    }
}
