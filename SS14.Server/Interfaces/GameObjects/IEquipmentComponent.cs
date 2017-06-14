using SS14.Shared;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IEquipmentComponent
    {
        void RaiseEquipItem(IEntity item);
        void RaiseEquipItemInHand();
        void RaiseUnEquipItemToFloor(IEntity item);
        void RaiseUnEquipItemToHand(IEntity item);
        void RaiseUnEquipItemToSpecifiedHand(IEntity item, InventoryLocation hand);
    }
}
