using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    /// <summary>
    /// Keeps a reference to the current eye (camera) that the client is seeing though, and provides
    /// utility functions for the current eye.
    /// </summary>
    public interface IEyeManager
    {
        /// <summary>
        /// The current eye that is being used to render the game.
        /// </summary>
        /// <remarks>
        /// Setting this property to null will use the default eye.
        /// </remarks>
        IEye CurrentEye { get; set; }

        IViewportControl MainViewport { get; set; }

        /// <summary>
        /// The ID of the map on which the current eye is "placed".
        /// </summary>
        MapId CurrentMap { get; }

        /// <summary>
        /// A world-space box that is at LEAST the area covered by the viewport.
        /// May be larger due to say rotation.
        /// </summary>
        Box2 GetWorldViewport();

        /// <summary>
        /// Calculates the projection matrix to transform a point from camera space
        /// to UI screen space.
        /// </summary>
        /// <param name="projMatrix"></param>
        void GetScreenProjectionMatrix(out Matrix3 projMatrix);

        /// <summary>
        /// Projects a point from world space to UI screen space using the current camera.
        /// </summary>
        /// <param name="point">Point in world to transform.</param>
        /// <returns>Corresponding point in UI screen space.</returns>
        Vector2 WorldToScreen(Vector2 point);

        /// <summary>
        /// Projects a point from world space to UI screen space using the current camera.
        /// </summary>
        /// <param name="point">Point in world to transform.</param>
        /// <returns>Corresponding point in UI screen space.</returns>
        ScreenCoordinates CoordinatesToScreen(EntityCoordinates point);

        /// <summary>
        /// Unprojects a point from UI screen space to world space using the viewport under the screen coordinates.
        /// </summary>
        /// <remarks>
        /// The game exists on the 2D X/Y plane, so this function returns a point o the plane
        /// instead of a line segment.
        /// </remarks>
        /// <param name="point">Point on screen to transform.</param>
        /// <returns>Corresponding point in the world.</returns>
        MapCoordinates ScreenToMap(ScreenCoordinates point);

        /// <summary>
        /// Unprojects a point from UI screen space to world space using the main viewport.
        /// </summary>
        /// <remarks>
        /// The game exists on the 2D X/Y plane, so this function returns a point o the plane
        /// instead of a line segment.
        /// </remarks>
        /// <param name="point">Point on screen to transform.</param>
        /// <returns>Corresponding point in the world.</returns>
        MapCoordinates ScreenToMap(Vector2 point);

        void ClearCurrentEye();
        void Initialize();
    }
}
