using GameObject;
using GameObject.System;
using Lidgren.Network;
using SS13_Shared.GO;

namespace SGO.EntitySystems
{
    public class InventorySystem : EntitySystem
    {
        public InventorySystem(EntityManager em, EntitySystemManager esm)
            : base(em, esm)
        {
            EntityQuery = new EntityQuery();
            EntityQuery.OneSet.Add(typeof(NewInventoryComponent));
            EntityQuery.OneSet.Add(typeof(NewEquipmentComponent));
            EntityQuery.OneSet.Add(typeof(NewHandsComponent));

            EntitySystemManager.RegisterMessageType<InventorySystemPickUp>(this);
            EntitySystemManager.RegisterMessageType<InventorySystemDrop>(this);
            EntitySystemManager.RegisterMessageType<InventorySystemExchange>(this);
        }

        public override void HandleNetMessage(EntitySystemMessage sysMsg)
        {
            if (sysMsg is InventorySystemPickUp)
            {
                InventorySystemPickUp message = sysMsg as InventorySystemPickUp;
                Entity user = EntityManager.GetEntity(message.uidUser);
                Entity obj = EntityManager.GetEntity(message.uidObject);
                if (user != null && obj != null)
                {
                    NewHandsComponent userHands = user.GetComponent<NewHandsComponent>(ComponentFamily.Hands);
                    BasicItemComponent objItem = obj.GetComponent<BasicItemComponent>(ComponentFamily.Item);

                    if (userHands != null && objItem != null)
                    {
                        if (userHands.handslots.ContainsKey(userHands.currentHand) && userHands.handslots[userHands.currentHand] == null)
                        {
                            
                        }
                    }
                    else if (userHands == null && objItem != null && obj.HasComponent(ComponentFamily.Inventory))
                    {
                        
                    }
                }
            }

            else if (sysMsg is InventorySystemDrop)
            {
                InventorySystemDrop message = sysMsg as InventorySystemDrop;
                Entity user = EntityManager.GetEntity(message.uidUser);
                Entity obj = EntityManager.GetEntity(message.uidObject);
                Entity dropping = EntityManager.GetEntity(message.uidDroppingInventory);
                if (user != null && obj != null && dropping != null)
                {

                }
            }

            else if (sysMsg is InventorySystemExchange)
            {
                InventorySystemExchange message = sysMsg as InventorySystemExchange;
                Entity user = EntityManager.GetEntity(message.uidUser);
                Entity obj = EntityManager.GetEntity(message.uidObject);
                Entity prevInv = EntityManager.GetEntity(message.uidPreviousInventory);
                Entity newInv = EntityManager.GetEntity(message.uidNewInventory);
                if (user != null && obj != null && prevInv != null && newInv != null)
                {

                }
            }
        }

        public override void Update(float frametime)
        {
        }
    }
}