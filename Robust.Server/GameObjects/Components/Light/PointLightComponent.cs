using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    public class PointLightComponent : Component
    {
        private Color _color;
        private bool _enabled;
        private float _radius;
        private Vector2 _offset;

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
                if (_enabled != value)
                {
                    _enabled = value;
                    Dirty();
                }
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

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _enabled, "enabled", true);
            serializer.DataField(ref _color, "color", new Color(200, 200, 200));
            serializer.DataField(ref _radius, "radius", 10);
            serializer.DataField(ref _offset, "offset", Vector2.Zero);
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new PointLightComponentState(Enabled, Color, Radius, Offset);
        }
    }
}
