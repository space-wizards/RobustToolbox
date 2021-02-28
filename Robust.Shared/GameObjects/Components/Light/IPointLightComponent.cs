using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects
{
    public interface IPointLightComponent
    {
        Color Color { get; set; }
        Vector2 Offset { get; set; }
        bool Enabled { get; set; }
        bool ContainerOccluded { get; set; }

        /// <summary>
        ///     Determines if the light mask should automatically rotate with the entity. (like a flashlight)
        /// </summary>
        bool MaskAutoRotate { get; set; }

        /// <summary>
        ///     Local rotation of the light mask around the center origin
        /// </summary>
        Angle Rotation { get; set; }

        /// <summary>
        /// The resource path to the mask texture the light will use.
        /// </summary>
        string? MaskPath { get; set; }

        float Energy { get; set; }

        /// <summary>
        ///     Soft shadow strength multiplier.
        ///     Has no effect if soft shadows are not enabled.
        /// </summary>
        float Softness { get; set; }

        bool VisibleNested { get; set; }

        /// <summary>
        ///     Radius, in meters.
        /// </summary>
        float Radius { get; set; }
    }
}
