using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.ViewVariables;

namespace Robust.Client.Graphics
{
    [DataDefinition]
    public sealed class NightVision
    {
        [DataField("color", customTypeSerializer:typeof(ColorSerializer)), ViewVariables(VVAccess.ReadWrite)] public Color Color = Color.White;
        [DataField("range"), ViewVariables(VVAccess.ReadWrite)] public float Range = 0.5f;
        [DataField("power"), ViewVariables(VVAccess.ReadWrite)] public float Power = 0.2f;
        [DataField("minExposure"), ViewVariables(VVAccess.ReadWrite)] public float MinExposure = 2.0f;
        /// <summary>
        /// How much very bright lights bother this eye. 0.1 means we can handle very bright lighting.
        ///   2.0 means everything turns white
        /// </summary>
        [DataField("lightIntolerance"), ViewVariables(VVAccess.ReadWrite)] public float LightIntolerance = 0.5f;
    }

    [DataDefinition]
    public sealed class AutoExpose
    {
        [DataField("min"), ViewVariables(VVAccess.ReadWrite)]
        public float Min = 0.4f;
        [DataField("max"), ViewVariables(VVAccess.ReadWrite)]
        public float Max = 4.0f;            // 12 is a good limit for quite reasonable nightvision.
        [DataField("rampDown"), ViewVariables(VVAccess.ReadWrite)]
        public float RampDown = 0.2f;
        [DataField("rampDownNight"), ViewVariables(VVAccess.ReadWrite)]
        public float RampDownNight = 1.0f; // Lose night vision quite fast
        [DataField("rampUp"), ViewVariables(VVAccess.ReadWrite)]
        public float RampUp = 0.025f;
        [DataField("rampUpNight"), ViewVariables(VVAccess.ReadWrite)]
        public float RampUpNight = 0.0015f; // As the eyes start straining, how fast do you adjust? (exposure / sec)

        [DataField("reduction"), ViewVariables(VVAccess.ReadWrite)]
        public float Reduction = 0.0f; // If you put on sunglasses, increase this (and decrease exposure the same)

        /// <summary>
        /// How bright you want the lights to appear in the centre of the screen when lights are bright
        /// </summary>
        [DataField("goalBrightness"), ViewVariables(VVAccess.ReadWrite)]
        public float GoalBrightness = 1.1f;

        /// <summary>
        /// How bright you want the lights to appear in the centre of the screen when lights are dim
        /// </summary>
        [DataField("goalBrightnessNight"), ViewVariables(VVAccess.ReadWrite)]
        public float GoalBrightnessNight = 0.60f;

        /// <summary>
        /// Renderer measurement of light intensity last frame. 0.1 is dark, 1.0 is extremely bright.
        ///   Note that this is after exposure is applied, so adjusting exposure each frame to keep this around
        ///   50-70% makes sense.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)] public float LastBrightness = 1.0f;
    }

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
        /// </summary>
        public float Exposure { get; set; }

        public NightVision? Night { get; set; }
        public AutoExpose? AutoExpose { get; set; }
    }
}
