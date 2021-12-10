using Robust.Client.Graphics;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Client.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedPointLightComponent))]
    public class PointLightComponent : SharedPointLightComponent, ISerializationHooks
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        internal bool TreeUpdateQueued { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public override Color Color
        {
            get => _color;
            set => base.Color = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public override bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                base.Enabled = value;
                _entityManager.EventBus.RaiseLocalEvent(Owner, new PointLightUpdateEvent());
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ContainerOccluded
        {
            get => _containerOccluded;
            set
            {
                if (_containerOccluded == value) return;

                _containerOccluded = value;
                _entityManager.EventBus.RaiseLocalEvent(Owner, new PointLightUpdateEvent());
            }
        }

        private bool _containerOccluded;

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

        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public float Energy
        {
            get => _energy;
            set => _energy = value;
        }

        /// <summary>
        ///     Soft shadow strength multiplier.
        ///     Has no effect if soft shadows are not enabled.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public float Softness
        {
            get => _softness;
            set => _softness = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool VisibleNested
        {
            get => _visibleNested;
            set => _visibleNested = value;
        }

        /// <summary>
        ///     Whether this pointlight should cast shadows
        /// </summary>
        [DataField("castShadows")]
        public bool CastShadows = true;

        [DataField("nestedvisible")]
        private bool _visibleNested = true;
        [DataField("autoRot")]
        private bool _maskAutoRotate;
        private Angle _rotation;
        [DataField("energy")]
        private float _energy = 1f;
        [DataField("softness")]
        private float _softness = 1f;
        [DataField("mask")]
        internal string? _maskPath;

        /// <summary>
        ///     Radius, in meters.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public override float Radius
        {
            get => _radius;
            set
            {
                if (MathHelper.CloseToPercent(value, _radius)) return;

                base.Radius = value;
                _entityManager.EventBus.RaiseEvent(EventSource.Local, new PointLightRadiusChangedEvent(this));
            }
        }

        [ViewVariables]
        internal RenderingTreeComponent? RenderTree { get; set; }
    }

    public class PointLightRadiusChangedEvent : EntityEventArgs
    {
        public PointLightComponent PointLightComponent { get; }

        public PointLightRadiusChangedEvent(PointLightComponent pointLightComponent)
        {
            PointLightComponent = pointLightComponent;
        }
    }

    public sealed class PointLightUpdateEvent : EntityEventArgs
    {

    }
}
