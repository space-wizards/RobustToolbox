using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Interfaces.Graphics.ClientEye
{
    /// <summary>
    ///     An Eye is a point through which the player can view the world.
    ///     It's a 2D camera in other game dev lingo basically.
    /// </summary>
    public interface IEye
    {
        /// <summary>
        /// Current zoom level of this eye. Zoom is the inverse of Scale (Zoom = 1 / Scale).
        /// </summary>
        Vector2 Zoom { get; set; }

        /// <summary>
        /// Current position of the center of the eye in the game world.
        /// </summary>
        MapCoordinates Position { get; }

        /// <summary>
        /// Returns the view matrix for this eye.
        /// </summary>
        Matrix3 GetViewMatrix();

        bool Is3D { get; }
    }
}
