using System;
using System.Numerics;
using Robust.Shared.Animations;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [NetworkedComponent, Access(typeof(SharedPointLightSystem))]
    public abstract partial class SharedPointLightComponent : Component, IComponentTreeEntry<SharedPointLightComponent>
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
        ///     The default value of 6.8 might seem suspect, but that's because this is a value which was introduced
        ///     years after SS14 already standardized its light values using an older attenuation curve, and this was the value
        ///     which, qualitatively, seemed about equivalent in brightness for the large majority of lights on the station
        ///     compared to the old function.
        ///
        ///     See https://www.desmos.com/calculator/yjudaha0s6 for a demonstration of how this value affects the shape of the curve
        ///     for different light radii and curve factors.
        /// </remarks>
        [DataField, Animatable]
        public float Falloff { get; set; } = 6.8f;

        /// <summary>
        ///     Controls the shape of the curve used for point light attenuation.
        ///     This value may vary between 0 and 1.
        ///     A value of 0 gives a shape roughly equivalent to 1/1+distance (more or less realistic),
        ///     while a value of 1 gives a shape roughly equivalent to 1+distance^2 (closer to a sphere-shaped light)
        /// </summary>
        /// <remarks>
        ///     This does not directly control the exponent of the denominator, though it might seem that way.
        ///     Rather, it just lerps between an inverse-shaped curve and an inverse-quadratic-shaped curve.
        ///     Values below 0 or above 1 are nonsensical.
        ///
        ///     See https://www.desmos.com/calculator/yjudaha0s6 for a demonstration of how this value affects the shape of the curve
        ///     for different light radii and falloff values.
        /// </remarks>
        [DataField, Animatable]
        public float CurveFactor { get; set; } = 0.0f;

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
        /// The Prototype ID of the light mask the light uses.
        /// </summary>
        [DataField("lightMask")]
        public ProtoId<LightMaskPrototype>? LightMask;

        #region Component Tree

        /// <inheritdoc />
        [ViewVariables]
        public EntityUid? TreeUid { get; set; }

        /// <inheritdoc />
        [ViewVariables]
        public DynamicTree<ComponentTreeEntry<SharedPointLightComponent>>? Tree { get; set; }

        /// <inheritdoc />
        [ViewVariables]
        public bool AddToTree => Enabled && !ContainerOccluded;

        /// <inheritdoc />
        [ViewVariables]
        public bool TreeUpdateQueued { get; set; }

        #endregion
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
}
