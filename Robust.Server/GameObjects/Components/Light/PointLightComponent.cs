using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(IPointLightComponent))]
    [NetID()]
    public class PointLightComponent : Component, IPointLightComponent
    {
        [DataField("color")]
        private Color _color = new(200, 200, 200);
        [DataField("enabled")]
        private bool _enabled = true;
        [DataField("radius")]
        private float _radius = 10;
        [DataField("offset")]
        private Vector2 _offset = Vector2.Zero;

        public override string Name => "PointLight";

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
                if (_enabled != value)
                {
                    _enabled = value;
                    Dirty();
                }
            }
        }

        public bool ContainerOccluded { get; set; }
        public bool MaskAutoRotate { get; set; }
        public Angle Rotation { get; set; }
        public string? MaskPath { get; set; }
        public float Energy { get; set; }
        public float Softness { get; set; }
        public bool VisibleNested { get; set; }

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

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new PointLightComponentState(Enabled, Color, Radius, Offset);
        }
    }
}
