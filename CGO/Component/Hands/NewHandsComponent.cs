using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.UserInterface;
using GameObject;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    public class NewHandsComponent : Component
    {
        public NewHandsComponent()
        {
            HandSlots = new Dictionary<InventoryLocation, Entity>();
            Family = ComponentFamily.Hands;
        }

        public Dictionary<InventoryLocation, Entity> HandSlots { get; private set; }
        public InventoryLocation CurrentHand { get; private set; }

        public override void HandleComponentState(dynamic state)
        {
            CurrentHand = state.ActiveHand;
            Dictionary<InventoryLocation, Entity> newInventory = new Dictionary<InventoryLocation, Entity>();
            foreach (KeyValuePair<InventoryLocation, int> curr in state.Slots)
            {
                Entity retEnt = Owner.EntityManager.GetEntity(curr.Value);
                newInventory.Add(curr.Key, retEnt);
            }

            //Find differences and raise event?

            HandSlots = newInventory;
        }
    }
}