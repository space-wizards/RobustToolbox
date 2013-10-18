using GameObject;
using GameObject.System;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO.EntitySystems
{
    public class InventorySystem : EntitySystem
    {
        public InventorySystem(EntityManager em, EntitySystemManager esm)
            : base(em, esm)
        {
            EntityQuery = new EntityQuery();
            EntityQuery.OneSet.Add(typeof (NewInventoryComponent));
            EntityQuery.OneSet.Add(typeof (NewEquipmentComponent));
            EntityQuery.OneSet.Add(typeof (NewHandsComponent));
        }

        public override void RegisterMessageTypes()
        {
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

                if(user != null && obj != null)
                    PickUpEntity(user, obj);
            }

            else if (sysMsg is InventorySystemDrop)
            {
                InventorySystemDrop message = sysMsg as InventorySystemDrop;
                Entity user = EntityManager.GetEntity(message.uidUser);
                Entity obj = EntityManager.GetEntity(message.uidObject);
                Entity dropping = EntityManager.GetEntity(message.uidDroppingInventory);

                if (user != null && obj != null && dropping != null)
                    RemoveEntity(user, dropping, obj);
            }

            else if (sysMsg is InventorySystemExchange) //TODO: Add argument for target inventory type.
            {
                InventorySystemExchange message = sysMsg as InventorySystemExchange;
                Entity user = EntityManager.GetEntity(message.uidUser);
                Entity obj = EntityManager.GetEntity(message.uidObject);
                Entity prevInv = EntityManager.GetEntity(message.uidPreviousInventory);
                Entity newInv = EntityManager.GetEntity(message.uidNewInventory);

                if (user != null && obj != null && prevInv != null && newInv != null)
                    ExchangeEntity(user, prevInv, newInv, obj);
            }
        }

        public override void Update(float frametime)
        {
        }

        #region Inventory Management Methods
        public bool PickUpEntity(Entity user, Entity obj)
        {
            NewHandsComponent userHands = user.GetComponent<NewHandsComponent>(ComponentFamily.Hands);
            BasicItemComponent objItem = obj.GetComponent<BasicItemComponent>(ComponentFamily.Item);

            if (userHands != null && objItem != null)
            {
                return AddEntity(user, user, obj, userHands.currentHand);
            }
            else if (userHands == null && objItem != null && obj.HasComponent(ComponentFamily.Inventory))
            {
                return AddEntity(user, user, obj, InventoryLocation.Inventory);
            }

            return false;
        }

        public bool ExchangeEntity(Entity user, Entity prevInventory, Entity newInventory, Entity obj)
        {
            if (!RemoveEntity(user, prevInventory, obj)) return false;
            if (!PickUpEntity(newInventory, obj)) return false;
            return true;
        } 

        public bool RemoveEntity(Entity user, Entity inventory, Entity toRemove, InventoryLocation location = InventoryLocation.Any)
        {
            NewHandsComponent comHands = inventory.GetComponent<NewHandsComponent>(ComponentFamily.Hands);
            NewEquipmentComponent comEquip = inventory.GetComponent<NewEquipmentComponent>(ComponentFamily.Equipment);
            NewInventoryComponent comInv = inventory.GetComponent<NewInventoryComponent>(ComponentFamily.Inventory);

            if ((location == InventoryLocation.Inventory) && comInv != null)
            {
                if (comInv.RemoveEntity(user, toRemove))
                {
                    //Do sprite stuff and detaching
                    return true;
                }
            }
            else if ((location == InventoryLocation.HandLeft || location == InventoryLocation.HandRight) && comHands != null)
            {
                if (comHands.RemoveEntity(user, toRemove))
                {
                    //Do sprite stuff and detaching
                    return true;
                }
            }
            else if ((location == InventoryLocation.Equipment || location == InventoryLocation.Any) && comEquip != null)
            {
                if (comEquip.RemoveEntity(user, toRemove))
                {
                    //Do sprite stuff and detaching
                    EquippableComponent eqCompo = toRemove.GetComponent<EquippableComponent>(ComponentFamily.Equippable);
                    if(eqCompo != null) eqCompo.currentWearer = null;
                    return true;
                }
            }
            else if (location == InventoryLocation.Any)
            {
                //Do sprite stuff and detaching
                bool done = false;

                if (comInv != null)
                    done = comInv.RemoveEntity(user, toRemove);

                if (comEquip != null && !done)
                    done = comEquip.RemoveEntity(user, toRemove);

                if (comHands != null && !done)
                    done = comHands.RemoveEntity(user, toRemove);

                return done;
            }

            return false;
        }

        public bool AddEntity(Entity user, Entity inventory, Entity toAdd, InventoryLocation location = InventoryLocation.Any)
        {
            NewHandsComponent comHands = inventory.GetComponent<NewHandsComponent>(ComponentFamily.Hands);
            NewEquipmentComponent comEquip = inventory.GetComponent<NewEquipmentComponent>(ComponentFamily.Equipment);
            NewInventoryComponent comInv = inventory.GetComponent<NewInventoryComponent>(ComponentFamily.Inventory);

            if ((location == InventoryLocation.Inventory) && comInv != null)
            {
                if (comInv.AddEntity(user, toAdd))
                {
                    //Do sprite stuff and attaching
                    return true;
                }
            }
            else if ((location == InventoryLocation.HandLeft || location == InventoryLocation.HandRight) && comHands != null)
            {
                if (comHands.AddEntity(user, toAdd, location))
                {
                    //Do sprite stuff and attaching
                    return true;
                }
            }
            else if ((location == InventoryLocation.Equipment || location == InventoryLocation.Any) && comEquip != null)
            {
                if (comEquip.AddEntity(user, toAdd))
                {
                    EquippableComponent eqCompo = toAdd.GetComponent<EquippableComponent>(ComponentFamily.Equippable);
                    eqCompo.currentWearer = user;
                    //Do sprite stuff and attaching.
                    return true;
                }
            }     
            else if (location == InventoryLocation.Any)
            {
                //Do sprite stuff and attaching.
                bool done = false;

                if (comInv != null)
                    done = comInv.AddEntity(user, toAdd);

                if (comEquip != null && !done)
                    done = comEquip.AddEntity(user, toAdd);

                if (comHands != null && !done)
                    done = comHands.AddEntity(user, toAdd, location);

                return done;
            }

            return false;
        }

        #endregion
    }
}