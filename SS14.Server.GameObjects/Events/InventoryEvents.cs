using SS14.Shared;
using SS14.Shared.GO;
using SS14.Shared.GameObjects;

namespace SS14.Server.GameObjects.Events
{
    public class InventoryPickedUpItemEventArgs : EntityEventArgs
    {
        public Entity Actor;
        public Entity Item;
    }

    public class InventoryDroppedItemEventArgs : EntityEventArgs
    {
        public Entity Actor;
        public Entity Item;
    }

    public class InventoryExchangedItemEventArgs : EntityEventArgs
    {
        public Entity Actor;
        public Entity Item1;
        public Entity Item2;
    }

    public class InventoryRemovedItemEventArgs : EntityEventArgs
    {
        public Entity Actor;
        public Entity Holder;
        public Entity Item;
    }

    public class InventoryAddedItemEventArgs : EntityEventArgs
    {
        public Entity Actor;
        public Entity Holder;
        public Entity Item;
    }

    /// <summary>
    /// Describes an event where an item is to be equipped from an arbitrary location
    /// </summary>
    public class InventoryEquipItemEventArgs : EntityEventArgs
    {
        public Entity Actor;
        public Entity Item;
    }

    /// <summary>
    /// Describes an event where an item is to be equipped from an entity's active hand
    /// </summary>
    public class InventoryEquipItemInHandEventArgs : EntityEventArgs
    {
        public Entity Actor;
    }

    /// <summary>
    /// Describes an event where an equipped item should be unequipped and dropped on the floor
    /// </summary>
    public class InventoryUnEquipItemToFloorEventArgs : EntityEventArgs
    {
        public Entity Actor;
        public Entity Item;
    }

    /// <summary>
    /// Describes an event where an item should be unequipped and placed into an entity's active hand
    /// </summary>
    public class InventoryUnEquipItemToHandEventArgs : EntityEventArgs
    {
        public Entity Actor;
        public Entity Item;
    }

    /// <summary>
    /// Describes an event where an item should be unequipped and placed into a specific hand
    /// </summary>
    public class InventoryUnEquipItemToSpecifiedHandEventArgs : EntityEventArgs
    {
        public Entity Actor;
        public Entity Item;
        public InventoryLocation Hand;
    }

    /// <summary>
    /// Describes an event where an item should be placed into an inventory
    /// </summary>
    public class InventoryAddItemToInventoryEventArgs : EntityEventArgs
    {
        public Entity Actor;
        public Entity Item;
    }
}
