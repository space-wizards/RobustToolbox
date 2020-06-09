using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Robust.Client.Placement
{
    public class PlacementHijack
    {
        public PlacementManager Manager { get; internal set; } = default!;

        public virtual bool HijackPlacementRequest(GridCoordinates coords)
        {
            return false;
        }

        public virtual bool HijackDeletion(IEntity entity)
        {
            return false;
        }

        public virtual void StartHijack(PlacementManager manager)
        {
            Manager = manager;
        }
    }
}
