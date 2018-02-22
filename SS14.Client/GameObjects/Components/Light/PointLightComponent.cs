using System;
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
using YamlDotNet.RepresentationModel;

namespace SS14.Client.GameObjects
{
    public class PointLightComponent : Component
    {
        public override string Name => "PointLight";
        public override uint? NetID => NetIDs.POINT_LIGHT;
        public override Type StateType => typeof(PointLightComponentState);

        private ILight Light;
        [Dependency]
        private ILightManager lightManager;

        public Color Color
        {
            get => Light.Color;
            set => Light.Color = value;
        }

        public Vector2 Offset
        {
            get => Light.Offset;
            set => Light.Offset = value;
        }

        private LightState state = LightState.On;
        public LightState State
        {
            get => state;
            set
            {
                state = value;
                Light.Enabled = state == LightState.On;
            }
        }

        public float Energy
        {
            get => Light.Energy;
            set => Light.Energy = value;
        }

        //private float texRadius;
        private float radius = 5;
        /// <summary>
        ///     Radius, in meters.
        /// </summary>
        public float Radius
        {
            get => radius;
            set
            {
                radius = FloatMath.Clamp(value, 2, 10);
                var mgr = IoCManager.Resolve<IResourceCache>();
                var tex = mgr.GetResource<TextureResource>($"Textures/Effects/Light/lighting_falloff_{(int)radius}.png");
                // TODO: Maybe editing the global texture resource is not a good idea.
                tex.Texture.GodotTexture.SetFlags(tex.Texture.GodotTexture.GetFlags() | (int)Godot.Texture.FlagsEnum.Filter);
                Light.Texture = tex.Texture;
            }
        }

        public override void Spawned()
        {
            lightManager = IoCManager.Resolve<ILightManager>();
            Light = lightManager.MakeLight();
        }

        public override void Initialize()
        {
            base.Initialize();

            var transform = Owner.GetComponent<IClientTransformComponent>();
            Light.ParentTo(transform);
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.TryGetNode("offset", out node))
            {
                Offset = node.AsVector2();
            }

            if (mapping.TryGetNode("radius", out node))
            {
                Radius = node.AsFloat();
            }

            if (mapping.TryGetNode("color", out node))
            {
                Color = node.AsHexColor();
            }

            if (mapping.TryGetNode("state", out node))
            {
                State = node.AsEnum<LightState>();
            }

            if (mapping.TryGetNode("energy", out node))
            {
                Energy = node.AsFloat();
            }
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
            var newState = (PointLightComponentState)state;
            State = newState.State;
            Color = newState.Color;
            Light.ModeClass = newState.Mode;
        }
    }
}
