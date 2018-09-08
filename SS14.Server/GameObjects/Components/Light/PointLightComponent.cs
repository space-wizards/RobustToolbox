using SS14.Shared.GameObjects;
using SS14.Shared.Enums;
using SS14.Shared.Maths;
using SS14.Shared.Serialization;
using SS14.Shared.ViewVariables;

namespace SS14.Server.GameObjects
{
    public class PointLightComponent : Component
    {
        private Color _color;
        private LightModeClass _mode;
        private LightState _state;
        private int _radius;
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
        public LightModeClass Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                Dirty();
            }
        }

        [ViewVariables]
        public LightState State
        {
            get => _state;
            set
            {
                _state = value;
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public int Radius
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

            serializer.DataField(ref _state, "state", LightState.On);
            serializer.DataField(ref _color, "color", new Color(200, 200, 200));
            serializer.DataField(ref _mode, "mode", LightModeClass.Constant);
            serializer.DataField(ref _radius, "radius", 512);
            serializer.DataField(ref _offset, "offset", Vector2.Zero);
        }

        public override ComponentState GetComponentState()
        {
            return new PointLightComponentState(State, Color, Mode, Radius, Offset);
        }
    }
}
