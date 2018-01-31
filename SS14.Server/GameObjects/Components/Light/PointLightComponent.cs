using SS14.Server.Interfaces.Chat;
using SS14.Shared;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    public class PointLightComponent : Component
    {
        public override string Name => "PointLight";
        public override uint? NetID => NetIDs.POINT_LIGHT;

        public Color Color { get; set; } = Color.White;
        public LightModeClass Mode { get; set; } = LightModeClass.Constant;
        public LightState State { get; set; } = LightState.On;
        public float Radius { get; set; } = 5;
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
