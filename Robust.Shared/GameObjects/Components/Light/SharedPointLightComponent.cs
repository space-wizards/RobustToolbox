using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System.Numerics;

namespace Robust.Shared.GameObjects
{
    [NetworkedComponent, Access(typeof(SharedPointLightSystem))]
    public abstract partial class SharedPointLightComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("color"), AutoNetworkedField]
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
        [DataField("energy"), AutoNetworkedField]
        [Animatable]
        public float Energy { get; set; } = 1f;

        [DataField("softness"), AutoNetworkedField, Animatable]
        public float Softness { get; set; } = 1f;

        /// <summary>
        ///     Whether this pointlight should cast shadows
        /// </summary>
        [DataField("castShadows"), AutoNetworkedField]
        public bool CastShadows = true;

        [Access(typeof(SharedPointLightSystem))]
        [DataField("enabled")]
        [Animatable]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// How far the light projects.
        /// </summary>
        [DataField("radius")]
        [Access(typeof(SharedPointLightSystem))]
        [Animatable]
        public float Radius { get; set; } = 5f;

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

    public sealed class PointLightToggleEvent : EntityEventArgs
    {
        public bool Enabled;

        public PointLightToggleEvent(bool enabled)
        {
            Enabled = enabled;
        }
    }
}
