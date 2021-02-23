using System;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Animations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Client.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(IPointLightComponent))]
    public class PointLightComponent : Component, IPointLightComponent, ISerializationHooks
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public override string Name => "PointLight";
        public override uint? NetID => NetIDs.POINT_LIGHT;

        internal bool TreeUpdateQueued { get; set; }

        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public Color Color
        {
            get => _color;
            set => _color = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Offset
        {
            get => _offset;
            set => _offset = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool ContainerOccluded { get; set; }

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

        /// <inheritdoc />
        /// <summary>
        /// The resource path to the mask texture the light will use.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string? MaskPath
        {
            get => _maskPath;
            set
            {
                _maskPath = value;
                UpdateMask();
            }
        }

        /// <summary>
        ///     Set a mask texture that will be applied to the light while rendering.
        ///     The mask's red channel will be linearly multiplied.p
        /// </summary>
        /// todo Paul: IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>(value)
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
            set
            {
                if (_visibleNested == value) return;
                _visibleNested = value;
                if (value)
                {
                    if (Owner.Transform.Parent == null) return;

                    _lightOnParent = true;
                }
                else
                {
                    if (!_lightOnParent) return;

                    _lightOnParent = false;
                }
            }
        }

        [DataField("radius")]
        private float _radius = 5f;
        [DataField("nestedvisible")]
        private bool _visibleNested = true;
        private bool _lightOnParent;
        [DataField("color")]
        private Color _color = Color.White;
        [DataField("offset")]
        private Vector2 _offset = Vector2.Zero;
        [DataField("enabled")]
        private bool _enabled = true;
        [DataField("autoRot")]
        private bool _maskAutoRotate;
        private Angle _rotation;
        [DataField("energy")]
        private float _energy = 1f;
        [DataField("softness")]
        private float _softness = 1f;
        [DataField("mask")]
        private string? _maskPath;

        /// <summary>
        ///     Radius, in meters.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [Animatable]
        public float Radius
        {
            get => _radius;
            set
            {
                _radius = MathF.Max(value, 0.01f); // setting radius to 0 causes exceptions, so just use a value close enough to zero that it's unnoticeable.
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new PointLightRadiusChangedMessage(this));
            }
        }

        private void UpdateMask()
        {
            if (_maskPath is not null)
                Mask = _resourceCache.GetResource<TextureResource>(_maskPath);
            else
                Mask = null;
        }

        public void AfterDeserialization()
        {
            if (_maskPath != null)
            {
                Mask = IoCManager.Resolve<IResourceCache>().GetResource<TextureResource>(_maskPath);
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            UpdateMask();
        }

        /// <inheritdoc />
        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            if (message is ParentChangedMessage msg)
            {
                HandleTransformParentChanged(msg);
            }
        }

        private void HandleTransformParentChanged(ParentChangedMessage obj)
        {
            // TODO: this does not work for things nested multiply layers deep.
            if (!VisibleNested)
            {
                return;
            }

            if (obj.NewParent != null && obj.NewParent.IsValid())
            {
                _lightOnParent = true;
            }
            else
            {
                _lightOnParent = false;
            }
        }

        public override void OnRemove()
        {
            base.OnRemove();

            var map = Owner.Transform.MapID;
            if (map != MapId.Nullspace)
            {
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local,
                    new RenderTreeRemoveLightMessage(this, map));
            }
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState == null)
                return;

            var newState = (PointLightComponentState) curState;
            Enabled = newState.Enabled;
            Radius = newState.Radius;
            Offset = newState.Offset;
            Color = newState.Color;
        }
    }

    public struct PointLightRadiusChangedMessage
    {
        public PointLightComponent PointLightComponent { get; }

        public PointLightRadiusChangedMessage(PointLightComponent pointLightComponent)
        {
            PointLightComponent = pointLightComponent;
        }
    }
}
