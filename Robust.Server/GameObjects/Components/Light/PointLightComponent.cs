using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    public class PointLightComponent : Component
    {
        [YamlField("color")]
        private Color _color = new(200, 200, 200);
        [YamlField("enabled")]
        private bool _enabled = true;
        [YamlField("radius")]
        private float _radius = 10;
        [YamlField("offset")]
        private Vector2 _offset = Vector2.Zero;

        public override string Name => "PointLight";
        public override uint? NetID => NetIDs.POINT_LIGHT;

        [ViewVariables(VVAccess.ReadWrite)]
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                Dirty();
            }
        }

        [ViewVariables]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public float Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new PointLightComponentState(Enabled, Color, Radius, Offset);
        }
    }
}
