using OpenTK;
using OpenTK.Graphics;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    public class PointLightComponent : Component
    {
        public override string Name => "PointLight";
        public override uint? NetID => NetIDs.POINT_LIGHT;

        public Color4 Color { get; set; } = new Color4(200, 200, 200, 255);
        public LightModeClass Mode { get; set; } = LightModeClass.Constant;
        public LightState State { get; set; } = LightState.On;
        public int Radius { get; set; } = 512;
        public Vector2 Offset { get; set; } = Vector2.Zero;

        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.TryGetNode("offset", out node))
            {
                Offset = node.AsVector2();
            }

            if (mapping.TryGetNode("radius", out node))
            {
                Radius = node.AsInt();
            }

            if (mapping.TryGetNode("color", out node))
            {
                Color = node.AsHexColor();
            }

            if (mapping.TryGetNode("state", out node))
            {
                State = node.AsEnum<LightState>();
            }

            if (mapping.TryGetNode("mode", out node))
            {
                Mode = node.AsEnum<LightModeClass>();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new PointLightComponentState(State, Color, Mode, Radius, Offset);
        }
    }
}
