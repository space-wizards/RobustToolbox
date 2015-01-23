using SS14.Server.GameObjects.Events;
using SS14.Server.Interfaces.GOC;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.GO;
using EntityQuery = SS14.Shared.GameObjects.EntityQuery;

namespace SS14.Server.GameObjects.EntitySystems
{
    public class InventorySystem : EntitySystem
    {
        public InventorySystem(EntityManager em, EntitySystemManager esm)
            : base(em, esm)
        {
            EntityQuery = new EntityQuery();
            EntityQuery.OneSet.Add(typeof (InventoryComponent));
            EntityQuery.OneSet.Add(typeof (EquipmentComponent));
            EntityQuery.OneSet.Add(typeof (HumanHandsComponent));
        }

        public override void RegisterMessageTypes()
        {
            EntitySystemManager.RegisterMessageType<InventorySystemPickUp>(this);
            EntitySystemManager.RegisterMessageType<InventorySystemDrop>(this);
            EntitySystemManager.RegisterMessageType<InventorySystemExchange>(this);
        }

        public override void SubscribeEvents()
        {
            base.SubscribeEvents();
            EntityManager.SubscribeEvent<InventoryPickedUpItemEventArgs>
                (new EntityEventHandler<InventoryPickedUpItemEventArgs>(HandlePickUpItem), this);
            EntityManager.SubscribeEvent<InventoryDroppedItemEventArgs>
                (new EntityEventHandler<InventoryDroppedItemEventArgs>(HandleDropItem), this);
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

        #region events
        public void HandlePickUpItem(object sender, InventoryPickedUpItemEventArgs args)
        {
            PickUpEntity(args.Actor, args.Item);
        }
        public void HandleDropItem(object sender, InventoryDroppedItemEventArgs args)
        {
            //Check to see if the item is actually in a hand
            var actorHands = args.Actor.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);

            //We can drop either a specific item from any inventory location or any item from a hand
            var item = args.Item;

            var holdingHand = InventoryLocation.None;
            if (item != null)
            {
                holdingHand = actorHands.GetHand(item);
                //If not, do nothing
                if (holdingHand != InventoryLocation.HandLeft && holdingHand != InventoryLocation.HandRight)
                    return;
            }
            else
            {
                holdingHand = actorHands.CurrentHand;
                item = actorHands.GetEntity(holdingHand);
            }
            if(item != null) 
                RemoveEntity(args.Actor, args.Actor, item, holdingHand);
        }

        public void HandleExchangeItem(object sender, InventoryExchangedItemEventArgs args)
        {
            
        }

        public void HandleRemovedItem(object sender, InventoryRemovedItemEventArgs args)
        {
            
        }

        public void HandleAddedItem(object sender, InventoryAddedItemEventArgs args)
        {
            
        }
        #endregion

        #region Inventory Management Methods
        public bool PickUpEntity(Entity user, Entity obj)
        {
            HumanHandsComponent userHands = user.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            BasicItemComponent objItem = obj.GetComponent<BasicItemComponent>(ComponentFamily.Item);

            if (userHands != null && objItem != null)
            {
                return AddEntity(user, user, obj, userHands.CurrentHand);
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
            var comHands = inventory.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            var comEquip = inventory.GetComponent<EquipmentComponent>(ComponentFamily.Equipment);
            var comInv = inventory.GetComponent<InventoryComponent>(ComponentFamily.Inventory);

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
                    //Do sprite stuff and attaching
                    var toRemoveSlaveMover = toRemove.GetComponent<SlaveMoverComponent>(ComponentFamily.Mover);
                    if(toRemoveSlaveMover != null)
                    {
                        toRemoveSlaveMover.Detach();
                    }

                    if (toRemove.HasComponent(ComponentFamily.Renderable))
                    {
                        toRemove.GetComponent<IRenderableComponent>(ComponentFamily.Renderable).UnsetMaster();
                    }
                    toRemove.RemoveComponent(ComponentFamily.Mover);
                    toRemove.AddComponent(ComponentFamily.Mover, EntityManager.ComponentFactory.GetComponent<BasicMoverComponent>());
                    toRemove.GetComponent<BasicItemComponent>(ComponentFamily.Item).HandleDropped();
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
            var comHands = inventory.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            var comEquip = inventory.GetComponent<EquipmentComponent>(ComponentFamily.Equipment);
            var comInv = inventory.GetComponent<InventoryComponent>(ComponentFamily.Inventory);

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
                    toAdd.RemoveComponent(ComponentFamily.Mover);
                    toAdd.AddComponent(ComponentFamily.Mover, EntityManager.ComponentFactory.GetComponent<SlaveMoverComponent>());
                    toAdd.GetComponent<SlaveMoverComponent>(ComponentFamily.Mover).Attach(inventory);
                    if (toAdd.HasComponent(ComponentFamily.Renderable) && inventory.HasComponent(ComponentFamily.Renderable))
                    {
                        toAdd.GetComponent<IRenderableComponent>(ComponentFamily.Renderable).SetMaster(inventory);
                    }
                    toAdd.GetComponent<BasicItemComponent>(ComponentFamily.Item).HandlePickedUp(inventory, location);
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