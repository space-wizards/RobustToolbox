using System;
using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System.Numerics;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects
{
    [NetworkedComponent, Access(typeof(SharedPointLightSystem))]
    public abstract partial class SharedPointLightComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("color")]
        [Animatable]
        public Color Color { get; set; } = Color.White;

        /// <summary>
        /// Offset from the center of the entity.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("offset")]
        [Access(Other = AccessPermissions.ReadWriteExecute)]
        public Vector2 Offset = Vector2.Zero;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("energy")]
        [Animatable]
        public float Energy { get; set; } = 1f;

        [DataField("softness"), Animatable]
        public float Softness { get; set; } = 1f;

        /// <summary>
        ///     Controls how quickly the light falls off in power in its radius.
        ///     A higher value means a stronger falloff.
        /// </summary>
        /// <remarks>
        /// The default value of 6.8 might seem suspect, but that's because this is a value which was introduced
        /// years after SS14 already standardized its light values using an older attenuation curve, and this was the value
        /// which, qualitatively, seemed about equivalent in brightness for the large majority of lights on the station
        /// compared to the old function.
        /// </remarks>
        [DataField("falloff"), Animatable]
        public float Falloff { get; set; } = 6.8f;

        /// <see cref="PointLightAttenuationCurveType"/>
        [DataField("curveType")]
        public PointLightAttenuationCurveType CurveType { get; set; } = PointLightAttenuationCurveType.Inverse;

        /// <summary>
        ///     Whether this pointlight should cast shadows
        /// </summary>
        [DataField("castShadows")]
        public bool CastShadows = true;

        [Access(typeof(SharedPointLightSystem))]
        [DataField("enabled")]
        public bool Enabled = true;

        // TODO ECS animations
        [Animatable]
        public bool AnimatedEnable
        {
            [Obsolete]
            get => Enabled;

            [Obsolete]
            set => IoCManager.Resolve<IEntityManager>().System<SharedPointLightSystem>().SetEnabled(Owner, value, this);
        }

        // TODO ECS animations
        [Animatable]
        public float AnimatedRadius
        {
            [Obsolete]
            get => Radius;
            [Obsolete]
            set => IoCManager.Resolve<IEntityManager>().System<SharedPointLightSystem>().SetRadius(Owner, value, this);
        }

        /// <summary>
        /// How far the light projects.
        /// </summary>
        [DataField("radius")]
        [Access(typeof(SharedPointLightSystem))]
        public float Radius = 5f;

        [ViewVariables]
        public bool ContainerOccluded;

        /// <summary>
        ///     Determines if the light mask should automatically rotate with the entity. (like a flashlight)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)] [DataField("autoRot")]
        public bool MaskAutoRotate;

        /// <summary>
        ///     Local rotation of the light mask around the center origin
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public Angle Rotation { get; set; }

        /// <summary>
        /// The resource path to the mask texture the light will use.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("mask")]
        public string? MaskPath;
    }

    /// <summary>
    /// Raised directed on an entity when attempting to enable / disable it.
    /// </summary>
    [ByRefEvent]
    public record struct AttemptPointLightToggleEvent(bool Enabled)
    {
        public bool Cancelled;
    }

    public sealed class PointLightToggleEvent : EntityEventArgs
    {
        public bool Enabled;

        public PointLightToggleEvent(bool enabled)
        {
            Enabled = enabled;
        }
    }

    /// <summary>
    ///     Controls the curve used for point light attenuation.
    /// </summary>
    /// <remarks>
    ///     See https://www.desmos.com/calculator/a3mskal3yu for a comparison of the curves for different
    ///     light radii and falloff values, alongside a comparison with the old pointlight attenuation.
    /// </remarks>
    [Serializable, NetSerializable]
    public enum PointLightAttenuationCurveType : byte
    {
        /// <summary>
        ///     A curve roughly equivalent in shape to (1/(1+distance).
        ///     This is the default behavior, and is relatively physically accurate.
        ///     Has a consistent level of falloff across the radius of the light.
        /// </summary>
        Inverse = 1,
        /// <summary>
        ///     A curve roughly equivalent in shape to (1/(1+distance^2).
        ///     This curve is not particularly physically accurate, but is better for representing
        ///     "spherical point lights", e.g. a glowing orb, which want some degree of consistent light in
        ///     close to their object's center before falling off as normal.
        ///     This curve type is also generally brighter, and requires a higher <see cref="SharedPointLightComponent.Falloff"/> value
        ///     to be equivalent in brightness to <see cref="Inverse"/>.
        /// </summary>
        QuadraticInverse = 2,
    }
}
