using GameObject;
using GameObject.System;
using SS13_Shared.GO;

namespace SGO.EntitySystems
{
    public class InventorySystem : EntitySystem
    {
        public InventorySystem(EntityManager em)
            : base(em)
        {
            EntityQuery = new EntityQuery();
            EntityQuery.OneSet.Add(typeof(InventoryComponent));
            EntityQuery.OneSet.Add(typeof(EquipmentComponent));
            EntityQuery.OneSet.Add(typeof(HumanHandsComponent));

            //EntityManager.EntitySystemManager.RegisterMessageType<InventorySystemPickUp>(this);
            //EntityManager.EntitySystemManager.RegisterMessageType<InventorySystemDrop>(this);
            //EntityManager.EntitySystemManager.RegisterMessageType<InventorySystemExchange>(this);
        }

        public override void HandleNetMessage(EntitySystemMessage sysMsg)
        {
            //can't use a switch for this, not an integral value :(
            //I wish i could think of a better way to structure this all. These ifs make it really hard to read.

            if (sysMsg is InventorySystemPickUp)
            {
                InventorySystemPickUp message = sysMsg as InventorySystemPickUp;
                Entity user = EntityManager.GetEntity(message.uidUser);
                Entity obj = EntityManager.GetEntity(message.uidObject);
                if (user != null && obj != null)
                {
                    HumanHandsComponent userHands = user.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
                    BasicItemComponent objItem = obj.GetComponent<BasicItemComponent>(ComponentFamily.Item);

                    if (userHands != null && objItem != null)
                    {

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