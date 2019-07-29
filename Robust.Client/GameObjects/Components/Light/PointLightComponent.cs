using System;
using Robust.Client.Interfaces.Graphics.Lighting;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using ObjectSerializer = Robust.Shared.Serialization.ObjectSerializer;

namespace Robust.Client.GameObjects
{
    public class PointLightComponent : Component
    {
        public override string Name => "PointLight";
        public override uint? NetID => NetIDs.POINT_LIGHT;
        public override Type StateType => typeof(PointLightComponentState);

        private ILight Light;
#pragma warning disable 649
        [Dependency] private readonly ILightManager lightManager;
        [Dependency] private readonly IResourceCache _resourceCache;
#pragma warning restore 649

        [ViewVariables(VVAccess.ReadWrite)]
        public Color Color
        {
            get => Light.Color;
            set => Light.Color = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Offset
        {
            get => Light.Offset;
            set => Light.Offset = value;
        }

        private LightState state = LightState.On;

        [ViewVariables(VVAccess.ReadWrite)]
        public LightState State
        {
            get => state;
            set
            {
                state = value;
                Light.Enabled = state == LightState.On;
            }
        }

        /// <summary>
        ///     Determines if the light mask should automatically rotate with the entity. (like a flashlight)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool MaskAutoRotate { get; set; }

        /// <summary>
        ///     Local rotation of the light mask around the center origin
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Angle Rotation
        {
            get => Light.Rotation;
            set => Light.Rotation = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Energy
        {
            get => Light.Energy;
            set => Light.Energy = value;
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

        private float radius = 5;
        private bool _visibleNested = true;
        private bool _lightOnParent = false;

        /// <summary>
        ///     Radius, in meters.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Radius
        {
            get => radius;
            set
            {
                radius = FloatMath.Clamp(value, 2, 10);
                //var tex = _resourceCache.GetResource<TextureResource>(new ResourcePath("/Textures/Effects/Light/") /
                //                                           $"lighting_falloff_{(int) radius}.png");


                //Light.Texture = tex.Texture;
            }
        }

        /// <inheritdoc />
        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            if ((message is ParentChangedMessage msg))
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

        public override void ExposeData(ObjectSerializer serializer)
        {
            // First in the init stack so...
            // FIXME: This is terrible.
            Light?.Dispose();
            Light = lightManager.MakeLight();
            serializer.DataReadWriteFunction("offset", Vector2.Zero, vec => Offset = vec, () => Offset);
            serializer.DataReadWriteFunction("radius", 5f, radius => Radius = radius, () => Radius);
            serializer.DataReadWriteFunction("color", Color.White, col => Color = col, () => Color);
            serializer.DataReadWriteFunction("state", LightState.On, state => State = state, () => State);
            serializer.DataReadWriteFunction("energy", 1f, energy => Energy = energy, () => Energy);
            serializer.DataReadWriteFunction("autoRot", false, rot => MaskAutoRotate = rot, () => MaskAutoRotate);
            serializer.DataFieldCached(ref _visibleNested, "nestedvisible", true);
        }

        public override void OnRemove()
        {
            Light.Dispose();
            Light = null;

            base.OnRemove();
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState curState, ComponentState nextState)
        {
            if (curState == null)
                return;

            var newState = (PointLightComponentState) curState;
            State = newState.State;
            Color = newState.Color;
            Light.ModeClass = newState.Mode;
        }
    }
}
