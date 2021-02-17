using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Robust.Server.GameObjects
{
    public class EyeComponent : SharedEyeComponent
    {
        private bool _drawFov;
        private Vector2 _zoom;
        private Vector2 _offset;
        private Angle _rotation;

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

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new EyeComponentState(DrawFov, Zoom, Offset, Rotation);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _zoom, "zoom", Vector2.One/2f);
            serializer.DataFieldCached(ref _drawFov, "drawFov", true);
        }
    }
}
