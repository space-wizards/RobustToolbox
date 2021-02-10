using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics.Interfaces.Graphics.ClientEye
{
    /// <summary>
    /// An Eye is a point through which the player can view the world.
    /// It's a 2D camera in other game dev lingo basically.
    /// </summary>
    [PublicAPI]
    public interface IEye
    {
        /// <summary>
        /// Should the black FoV effect be drawn for this eye?
        /// </summary>
        bool DrawFov { get; set; }

        /// <summary>
        /// Current position of the center of the eye in the game world.
        /// </summary>
        MapCoordinates Position { get; }

        /// <summary>
        /// Rotation of the camera around the Z axis.
        /// </summary>
        Angle Rotation { get; set; }

        /// <summary>
        /// Current zoom level of this eye. Zoom is the inverse of Scale (Zoom = 1 / Scale).
        /// </summary>
        Vector2 Zoom { get; set; }

        /// <summary>
        /// Current view scale of this eye. Scale is the inverse of Zoom (Scale = 1 / Zoom).
        /// </summary>
        Vector2 Scale { get; set; }

        /// <summary>
        /// Returns the view matrix for this eye, used to convert a point from
        /// world space to camera space.
        /// </summary>
        /// <param name="viewMatrix">View matrix for this camera.</param>
        void GetViewMatrix(out Matrix3 viewMatrix);

        /// <summary>
        /// Returns the inverted view matrix for this eye, used to convert a point from
        /// camera space to world space.
        /// </summary>
        /// <param name="viewMatrixInv">Inverted view matrix for this camera.</param>
        void GetViewMatrixInv(out Matrix3 viewMatrixInv);
    }
}
