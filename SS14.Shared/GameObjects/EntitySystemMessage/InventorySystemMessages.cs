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
        private int uidUser;
        private int uidObject;

        public int UidUser { get => uidUser; set => uidUser = value; }
        public int UidObject { get => uidObject; set => uidObject = value; }

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
        private int uidUser;
        private int uidObject;
        private int uidDroppingInventory;

        public int UidUser { get => uidUser; set => uidUser = value; }
        public int UidObject { get => uidObject; set => uidObject = value; }
        public int UidDroppingInventory { get => uidDroppingInventory; set => uidDroppingInventory = value; }

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
        private int uidUser;
        private int uidObject;
        private int uidPreviousInventory;
        private int uidNewInventory;

        public int UidUser { get => uidUser; set => uidUser = value; }
        public int UidObject { get => uidObject; set => uidObject = value; }
        public int UidPreviousInventory { get => uidPreviousInventory; set => uidPreviousInventory = value; }
        public int UidNewInventory { get => uidNewInventory; set => uidNewInventory = value; }

        public InventorySystemExchange()
        {
        }
    }
}
