using System;
using System.Runtime.Remoting.Messaging;
using SS14.Client.Graphics.Lighting;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Shared;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using SS14.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using ObjectSerializer = SS14.Shared.Serialization.ObjectSerializer;

namespace SS14.Client.GameObjects
{
    public class PointLightComponent : Component
    {
        public override string Name => "PointLight";
        public override uint? NetID => NetIDs.POINT_LIGHT;
        public override Type StateType => typeof(PointLightComponentState);

        private ILight Light;
        [Dependency] private ILightManager lightManager;

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

                    if (GameController.OnGodot)
                    {
                        Light.ParentTo((GodotTransformComponent) Owner.Transform.Parent);
                    }

                    _lightOnParent = true;
                }
                else
                {
                    if (!_lightOnParent) return;

                    if (GameController.OnGodot)
                    {
                        Light.ParentTo((GodotTransformComponent) Owner.Transform);
                    }

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
                var mgr = IoCManager.Resolve<IResourceCache>();
                var tex = mgr.GetResource<TextureResource>(new ResourcePath("/Textures/Effects/Light/") /
                                                           $"lighting_falloff_{(int) radius}.png");

                if (GameController.OnGodot)
                {
                    // TODO: Maybe editing the global texture resource is not a good idea.
                    tex.Texture.GodotTexture.SetFlags(tex.Texture.GodotTexture.GetFlags() |
                                                      (int) Godot.Texture.FlagsEnum.Filter);
                }

                Light.Texture = tex.Texture;
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            if (GameController.OnGodot)
            {
                Light.ParentTo((GodotTransformComponent) Owner.Transform);
            }

            Owner.Transform.OnParentChanged += TransformOnOnParentChanged;
        }

        private void TransformOnOnParentChanged(ParentChangedEventArgs obj)
        {
            // TODO: this does not work for things nested multiply layers deep.
            if (!VisibleNested)
            {
                return;
            }

            if (obj.New.IsValid() && Owner.EntityManager.TryGetEntity(obj.New, out var entity))
            {
                if (GameController.OnGodot)
                {
                    Light.ParentTo((GodotTransformComponent) entity.Transform);
                }

                _lightOnParent = true;
            }
            else
            {
                if (GameController.OnGodot)
                {
                    Light.ParentTo((GodotTransformComponent) Owner.Transform);
                }

                _lightOnParent = false;
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            if (lightManager == null)
            {
                // First in the init stack so...
                // FIXME: This is terrible.
                lightManager = IoCManager.Resolve<ILightManager>();
                Light = lightManager.MakeLight();
            }

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
        public override void HandleComponentState(ComponentState state)
        {
            var newState = (PointLightComponentState) state;
            State = newState.State;
            Color = newState.Color;
            Light.ModeClass = newState.Mode;
        }
    }
}
