using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Graphics
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
        public bool DrawFov { get; set; }

        /// <summary>
        /// Current position of the center of the eye in the game world.
        /// </summary>
        MapCoordinates Position { get; }

        /// <summary>
        /// Translation offset from <see cref="Position"/>. Does not influence the center of FOV.
        /// </summary>
        Vector2 Offset { get; set; }

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
        /// <param name="renderScale"></param>
        void GetViewMatrix(out Matrix3 viewMatrix, Vector2 renderScale);

        /// <summary>
        /// Returns the inverted view matrix for this eye, used to convert a point from
        /// camera space to world space.
        /// </summary>
        /// <param name="viewMatrixInv">Inverted view matrix for this camera.</param>
        /// <param name="renderScale"></param>
        void GetViewMatrixInv(out Matrix3 viewMatrixInv, Vector2 renderScale);

        void GetViewMatrixNoOffset(out Matrix3 viewMatrix, Vector2 renderScale);

        /// <summary>
        /// How much we should brighten lights around the player. 1.0 is default brightness
        ///   This is a multiplier to light power and it also increases range by sqrt(Exposure) as per light laws..
        /// </summary>
        public float Exposure { get; set; }

        /// <summary>
        /// Renderer measurement of light intensity last frame. 0.1 is dark, 1.0 is extremely bright.
        ///   The renderer measures a tiny square in the very centre of the viewport.
        ///   Note that this is after exposure is applied, so adjusting exposure each frame to keep this around
        ///   50-70% makes sense.
        /// </summary>
        public float LastBrightness { get; set; }

        /// <summary>
        /// Set true if you want to use LastBrightness.
        ///   The renderer will read the lighting texture and calculate it for you each frame.
        /// </summary>
        public bool MeasureBrightness { get; set; }

        /// <summary>
        /// Lighting over 100% is compressed by taking sqrt and then multiplying by this value.
        ///   A value like 0.5 looks nice, 0.05 crushes bright light down to near-fullbright.
        ///   0.0 crushes it to exactly fullbright, which is bland.
        /// Important when there are LOT of lightsources, or they are powerful, or Exposure is high.
        /// </summary>
        public float LightIntolerance { get; set; }

    }
}
