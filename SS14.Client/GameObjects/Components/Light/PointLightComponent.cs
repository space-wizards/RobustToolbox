using OpenTK;
using OpenTK.Graphics;
using Lidgren.Network;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Client.Interfaces.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.ResourceManagement;
using SS14.Client.Utility;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using Vector2 = SS14.Shared.Maths.Vector2;
using SS14.Client.Interfaces.Graphics.Lighting;
using SS14.Client.Graphics.Lighting;
using SS14.Shared.Log;

namespace SS14.Client.GameObjects
{
    public class PointLightComponent : Component
    {
        public override string Name => "PointLight";
        public override uint? NetID => NetIDs.POINT_LIGHT;
        public override Type StateType => typeof(PointLightComponentState);

        private ILight Light;

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
                tex.Texture.Texture.SetFlags(tex.Texture.Texture.GetFlags() | (int)Godot.Texture.Flags.Filter);
                Light.Texture = tex.Texture;
            }
        }

        public override void Spawned()
        {
            // TODO: move this pixels per meter somewhere else like a centralized camera system.
            //const float PixelsPerMeter = 32;
            // Yes, this only cares about X axis. I know and don't care.
            //texRadius = res.Texture.Texture.GetWidth() / PixelsPerMeter;

            Light = new Light();
        }

        public override void Initialize()
        {
            base.Initialize();

            var transform = Owner.GetComponent<IClientTransformComponent>();
            Light.ParentTo(transform.SceneNode);
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

