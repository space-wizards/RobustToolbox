using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Animations;
using Robust.Shared.ComponentTrees;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using TerraFX.Interop.Windows;

namespace Robust.Client.GameObjects
{
    public struct PointLight
    {
        // Only the values required to actually render the light.
        public Color Color;
        public bool MaskAutoRotate;
        public Angle Rotation;
        public Texture? Mask;
        public float Radius;
        public float Energy;
        public bool CastShadows;
        public float Softness;

        // These are calculated by lighting engine after transforming from the above.
        public Vector2 ScreenPosition;
        public float DistFromCentreSq;
        public Angle ScreenRotation;

        public PointLight(PointLightComponent light)
        {
            Color = light.Color;
            MaskAutoRotate = light.MaskAutoRotate;
            Rotation = light.Rotation;
            Mask = light.Mask;
            Radius = light.Radius;
            Energy = light.Energy;
            CastShadows = light.CastShadows;
            Softness = light.Softness;
        }

        public PointLight(float energy, float radius, bool castShadows)
        {
            Energy = energy;
            Radius = radius;
            CastShadows = castShadows;
            Color = Color.White;
        }

        public void UpdateFrom(PointLightComponent light)
        {
            Color = light.Color;
            MaskAutoRotate = light.MaskAutoRotate;
            Rotation = light.Rotation;
            Mask = light.Mask;
            Radius = light.Radius;
            Energy = light.Energy;
            CastShadows = light.CastShadows;
            Softness = light.Softness;
        }

    }


    [RegisterComponent]
    [ComponentReference(typeof(SharedPointLightComponent))]
    public sealed class PointLightComponent : SharedPointLightComponent, IComponentTreeEntry<PointLightComponent>
    {
        public EntityUid? TreeUid { get; set; }

        public DynamicTree<ComponentTreeEntry<PointLightComponent>>? Tree { get; set; }

        public bool AddToTree => Enabled && !ContainerOccluded;
        public bool TreeUpdateQueued { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public override Color Color
        {
            get => _color;
            set => base.Color = value;
        }

        [Access(typeof(PointLightSystem))]
        public bool ContainerOccluded;

        /// <summary>
        ///     Determines if the light mask should automatically rotate with the entity. (like a flashlight)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool MaskAutoRotate
        {
            get => _maskAutoRotate;
            set => _maskAutoRotate = value;
        }

        /// <summary>
        ///     Local rotation of the light mask around the center origin
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public Angle Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }

        /// <summary>
        /// The resource path to the mask texture the light will use.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string? MaskPath
        {
            get => _maskPath;
            set
            {
                if (_maskPath?.Equals(value) != false) return;
                _maskPath = value;
                EntitySystem.Get<PointLightSystem>().UpdateMask(this);
            }
        }

        /// <summary>
        ///     Set a mask texture that will be applied to the light while rendering.
        ///     The mask's red channel will be linearly multiplied.p
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Texture? Mask { get; set; }

        [DataField("autoRot")]
        private bool _maskAutoRotate;
        private Angle _rotation;

        [DataField("mask")]
        internal string? _maskPath;
    }
}
