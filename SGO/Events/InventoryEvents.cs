using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameObject;
using SS13_Shared;

namespace SGO.Events
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
