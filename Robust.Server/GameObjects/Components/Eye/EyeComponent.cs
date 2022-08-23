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
    [ComponentReference(typeof(SharedEyeComponent))]
    public sealed class EyeComponent : SharedEyeComponent
    {
        public const int DefaultVisibilityMask = 1;

        [DataField("drawFov")]
        private bool _drawFov = true;
        [DataField("zoom")]
        private Vector2 _zoom = Vector2.One;
        private Vector2 _offset;
        private Angle _rotation;
        private uint _visibilityMask = DefaultVisibilityMask;

        public override bool DrawFov
        {
            get => _drawFov;
            set
            {
                if (_drawFov == value)
                    return;

                _drawFov = value;
                Dirty();
            }
        }

        public override Vector2 Zoom
        {
            get => _zoom;
            set
            {
                if (_zoom.EqualsApprox(value))
                    return;

                _zoom = value;
                Dirty();
            }
        }

        public override Vector2 Offset
        {
            get => _offset;
            set
            {
                if (_offset.EqualsApprox(value))
                    return;

                _offset = value;
                Dirty();
            }
        }

        public override Angle Rotation
        {
            get => _rotation;
            set
            {
                if(_rotation.EqualsApprox(value))
                    return;

                _rotation = value;
                Dirty();
            }
        }

        public override uint VisibilityMask
        {
            get => _visibilityMask;
            set
            {
                if(_visibilityMask == value)
                    return;

                _visibilityMask = value;
                Dirty();
            }
        }

        public override ComponentState GetComponentState()
        {
            return new EyeComponentState(DrawFov, Zoom, Offset, Rotation, VisibilityMask);
        }
    }
}
