using System;

namespace SS14.Shared.GameObjects
{
    //Client sends this when it picks something up.
    //Server does not send this.
    //Action: Server sets objects location to hands compo if hand compo exists and action is valid,
    //        hand compo fires event that changes sprites, attaches etc. If no hands, try to move to inventory?
    [Serializable]
    public class InventorySystemPickUp : EntitySystemMessage
    {
        public int UidUser { get; set; }
        public int UidObject { get; set; }

        public InventorySystemPickUp()
        {
        }
    }

    //Client sends this when it drops something.
    //Server does not send this.
    //Action: Server sets object location to ground, removes object from all related compos - compos fire events that change sprite, detach etc.
    [Serializable]
    public class InventorySystemDrop : EntitySystemMessage
    {
        public int UidUser { get; set; }
        public int UidObject { get; set; }
        public int UidDroppingInventory { get; set; }

        public InventorySystemDrop()
        {
        }
    }

    //Client sends this to inform the server that an entity was moved from one container to another.
    //Server does not send this.
    //Action: Server moves object from one inventory compo to another - compos fire appropriate events.
    [Serializable]
    public class InventorySystemExchange : EntitySystemMessage
    {
        public int UidUser { get; set; }
        public int UidObject { get; set; }
        public int UidPreviousInventory { get; set; }
        public int UidNewInventory { get; set; }

        public InventorySystemExchange()
        {
        }
    }
}
