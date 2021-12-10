using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Robust.Client.Placement
{
    public class PlacementHijack
    {
        public PlacementManager Manager { get; internal set; } = default!;
        public virtual bool CanRotate { get; } = true;

        public virtual bool HijackPlacementRequest(EntityCoordinates coordinates)
        {
            return false;
        }

        public virtual bool HijackDeletion(EntityCoordinates coordinates)
        {
            return false;
        }

        public virtual bool HijackDeletion(EntityUid entity)
        {
            return false;
        }

        public virtual void StartHijack(PlacementManager manager)
        {
            Manager = manager;
        }
    }
}
