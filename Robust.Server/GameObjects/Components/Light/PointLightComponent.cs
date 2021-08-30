using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Robust.Server.GameObjects
{
    [RegisterComponent]
    [ComponentReference(typeof(IPointLightComponent)), ComponentReference(typeof(SharedPointLightComponent))]
    [NetworkedComponent]
    public class PointLightComponent : SharedPointLightComponent, IPointLightComponent
    {
        [DataField("color")]
        private Color _color = new(200, 200, 200);
        [DataField("enabled")]
        private bool _enabled = true;
        [DataField("radius")]
        private float _radius = 10;
        [DataField("offset")]
        private Vector2 _offset = Vector2.Zero;

        private bool _containerOccluded;

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

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                // Kinda weird until ECS lights
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new UpdatePVSRangeEvent
                {
                    Component = this,
                    Bounds = EntitySystem.Get<PointLightSystem>().GetPvsRange(this),
                });
                Dirty();
            }
        }

        public bool ContainerOccluded
        {
            get => _containerOccluded;
            set
            {
                if (_containerOccluded == value) return;
                _containerOccluded = value;
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new UpdatePVSRangeEvent
                {
                    Component = this,
                    Bounds = EntitySystem.Get<PointLightSystem>().GetPvsRange(this),
                });
            }
        }

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
                if (MathHelper.CloseTo(_radius, value)) return;
                _radius = value;
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new UpdatePVSRangeEvent
                {
                    Component = this,
                    Bounds = EntitySystem.Get<PointLightSystem>().GetPvsRange(this),
                });
                Dirty();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public Vector2 Offset
        {
            get => _offset;
            set
            {
                if (_offset.Equals(value)) return;
                _offset = value;
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, new UpdatePVSRangeEvent
                {
                    Component = this,
                    Bounds = EntitySystem.Get<PointLightSystem>().GetPvsRange(this),
                });
                Dirty();
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new PointLightComponentState(Enabled, Color, Radius, Offset);
        }
    }
}
