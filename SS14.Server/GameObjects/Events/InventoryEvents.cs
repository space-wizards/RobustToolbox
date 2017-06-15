using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;

namespace SS14.Server.GameObjects.Events
{
    public class InventoryPickedUpItemEventArgs : EntityEventArgs
    {
        public IEntity Actor;
        public IEntity Item;
    }

    public class InventoryDroppedItemEventArgs : EntityEventArgs
    {
        public IEntity Actor;
        public IEntity Item;
    }

    public class InventoryExchangedItemEventArgs : EntityEventArgs
    {
        public IEntity Actor;
        public IEntity Item1;
        public IEntity Item2;
    }

    public class InventoryRemovedItemEventArgs : EntityEventArgs
    {
        public IEntity Actor;
        public IEntity Holder;
        public IEntity Item;
    }

    public class InventoryAddedItemEventArgs : EntityEventArgs
    {
        public IEntity Actor;
        public IEntity Holder;
        public IEntity Item;
    }

    /// <summary>
    /// Describes an event where an item is to be equipped from an arbitrary location
    /// </summary>
    public class InventoryEquipItemEventArgs : EntityEventArgs
    {
        public IEntity Actor;
        public IEntity Item;
    }

    /// <summary>
    /// Describes an event where an item is to be equipped from an entity's active hand
    /// </summary>
    public class InventoryEquipItemInHandEventArgs : EntityEventArgs
    {
        public IEntity Actor;
    }

    /// <summary>
    /// Describes an event where an equipped item should be unequipped and dropped on the floor
    /// </summary>
    public class InventoryUnEquipItemToFloorEventArgs : EntityEventArgs
    {
        public IEntity Actor;
        public IEntity Item;
    }

    /// <summary>
    /// Describes an event where an item should be unequipped and placed into an entity's active hand
    /// </summary>
    public class InventoryUnEquipItemToHandEventArgs : EntityEventArgs
    {
        public IEntity Actor;
        public IEntity Item;
    }

    /// <summary>
    /// Describes an event where an item should be unequipped and placed into a specific hand
    /// </summary>
    public class InventoryUnEquipItemToSpecifiedHandEventArgs : EntityEventArgs
    {
        public IEntity Actor;
        public IEntity Item;
        public InventoryLocation Hand;
    }

    /// <summary>
    /// Describes an event where an item should be placed into an inventory
    /// </summary>
    public class InventoryAddItemToInventoryEventArgs : EntityEventArgs
    {
        public IEntity Actor;
        public IEntity Item;
    }
}
