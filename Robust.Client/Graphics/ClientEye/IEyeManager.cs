using System;
using System.Numerics;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Graphics;
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
        /// The primary eye, which is usually the eye associated with the main viewport.
        /// </summary>
        /// <remarks>
        /// Generally, you should avoid using this whenever possible. E.g., when rendering overlays should use the
        /// eye & viewbounds that gets passed to the draw method.
        /// Setting this property to null will use the default eye.
        /// </remarks>
        IEye CurrentEye { get; set; }

        IViewportControl MainViewport { get; set; }

        [Obsolete]
        MapId CurrentMap { get; }

        /// <summary>
        /// A world-space box that is at LEAST the area covered by the main viewport.
        /// May be larger due to say rotation.
        /// </summary>
        Box2 GetWorldViewport();

        /// <summary>
        /// A world-space box of the area visible in the main viewport.
        /// </summary>
        Box2Rotated GetWorldViewbounds();

        /// <summary>
        /// Calculates the projection matrix to transform a point from camera space
        /// to UI screen space.
        /// </summary>
        /// <param name="projMatrix"></param>
        void GetScreenProjectionMatrix(out Matrix3x2 projMatrix);

        /// <summary>
        /// Projects a point from world space to UI screen space using the main viewport.
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

        ScreenCoordinates MapToScreen(MapCoordinates point);

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

        /// <summary>
        /// Similar to <see cref="ScreenToMap(ScreenCoordinates)"/>, except it should compensate for the effects of shaders on viewports.
        /// </summary>
        MapCoordinates PixelToMap(ScreenCoordinates point);

        /// <summary>
        /// Similar to <see cref="ScreenToMap(Vector2)"/>, except it should compensate for the effects of shaders on viewports.
        /// </summary>
        MapCoordinates PixelToMap(Vector2 point);

        void ClearCurrentEye();
        void Initialize();
    }
}
