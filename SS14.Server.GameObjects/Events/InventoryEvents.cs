using SS14.Shared;
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
}
