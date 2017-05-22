using SS14.Shared;
using SS14.Shared.GameObjects;

namespace SS14.Server.Interfaces.GameObject
{
    public interface IEquipmentComponent
    {
        void RaiseEquipItem(Entity item);
        void RaiseEquipItemInHand();
        void RaiseUnEquipItemToFloor(Entity item);
        void RaiseUnEquipItemToHand(Entity item);
        void RaiseUnEquipItemToSpecifiedHand(Entity item, InventoryLocation hand);
    }
}
