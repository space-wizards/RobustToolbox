using System.Numerics;
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
        IClydeWindow? Window { get; }

        /// <summary>
        ///     Converts a point on the screen to map coordinates.
        /// </summary>
        /// <param name="coords">
        ///     The coordinates, in ABSOLUTE SCREEN PIXEL COORDINATES. NOT CONTROL-RELATIVE COORDINATES.
        /// </param>
        MapCoordinates ScreenToMap(Vector2 coords);

        /// <summary>
        /// Similar to <see cref="ScreenToMap(Vector2)"/>, except it should compensate for the effects of shaders on viewports.
        /// </summary>
        MapCoordinates PixelToMap(Vector2 point);

        /// <summary>
        ///     Converts a point on the map to screen coordinates.
        /// </summary>
        /// <returns>
        ///     The coordinates, in ABSOLUTE SCREEN PIXEL COORDINATES. NOT CONTROL-RELATIVE COORDINATES.
        /// </returns>
        Vector2 WorldToScreen(Vector2 map);

        /// <summary>
        ///     Returns a matrix that can be used to perform the <see cref="WorldToScreen(Vector2)"/> transformations.
        /// </summary>
        /// <remarks>
        ///     This is generally just be a combination of <see cref="IClydeViewport.GetWorldToLocalMatrix"/> and <see cref="GetLocalToScreenMatrix"/>
        /// </remarks>
        Matrix3x2 GetWorldToScreenMatrix();

        /// <summary>
        ///     Returns a matrix that can be used to transform from view-port local to screen coordinates.
        /// </summary>
        Matrix3x2 GetLocalToScreenMatrix();
    }
}
