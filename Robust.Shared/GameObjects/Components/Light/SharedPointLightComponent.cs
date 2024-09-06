using System;
using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System.Numerics;
using Robust.Shared.IoC;

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
}
