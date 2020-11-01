using Robust.Shared.GameObjects.Components.Eye;
using Robust.Shared.Serialization;

namespace Robust.Server.GameObjects.Components.Eye
{
    public class EyeComponent : SharedEyeComponent
    {
        private bool _drawFov;

        public override bool DrawFov
        {
            get => _drawFov;
            set
            {
                if (_drawFov == value)
                {
                    return;
                }

                _drawFov = value;
                Dirty();
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            
            serializer.DataFieldCached(ref _drawFov, "drawFov", true);
        }
    }
}