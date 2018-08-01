using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;

namespace SS14.Client.Placement
{
    public class PlacementHijack
    {
        public PlacementManager Manager { get; internal set; }

        public virtual bool HijackPlacementRequest(GridLocalCoordinates coords)
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
