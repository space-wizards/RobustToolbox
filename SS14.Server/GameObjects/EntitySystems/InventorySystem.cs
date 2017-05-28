using SS14.Server.GameObjects.Events;
using SS14.Server.Interfaces.GOC;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
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
            EntityManager.SubscribeEvent<InventoryEquipItemEventArgs>
                (new EntityEventHandler<InventoryEquipItemEventArgs>(HandleEquipItem), this);
            EntityManager.SubscribeEvent<InventoryEquipItemInHandEventArgs>
                (new EntityEventHandler<InventoryEquipItemInHandEventArgs>(HandleEquipItemInHand), this);
            EntityManager.SubscribeEvent<InventoryUnEquipItemToFloorEventArgs>
                (new EntityEventHandler<InventoryUnEquipItemToFloorEventArgs>(HandleUnEquipItemToFloor), this);
            EntityManager.SubscribeEvent<InventoryUnEquipItemToHandEventArgs>
                (new EntityEventHandler<InventoryUnEquipItemToHandEventArgs>(HandleUnEquipItemToHand), this);
            EntityManager.SubscribeEvent<InventoryUnEquipItemToSpecifiedHandEventArgs>
                (new EntityEventHandler<InventoryUnEquipItemToSpecifiedHandEventArgs>(HandleUnEquipItemToSpecifiedHand), this);
            EntityManager.SubscribeEvent<InventoryAddItemToInventoryEventArgs>
                (new EntityEventHandler<InventoryAddItemToInventoryEventArgs>(HandleAddItemToInventory), this);
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

            InventoryLocation holdingHand;
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

        public void HandleEquipItem(object sender, InventoryEquipItemEventArgs args)
        {
            var actor = args.Actor;
            var toEquip = args.Item;

            AddEntity(actor, actor, toEquip, InventoryLocation.Equipment);
        }

        public void HandleEquipItemInHand(object sender, InventoryEquipItemInHandEventArgs args)
        {
            var actor = args.Actor;
            var handsComp = actor.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            if(!handsComp.IsEmpty(handsComp.CurrentHand))
            {
                // TODO check if target slot can recieve entity
                var ent = handsComp.GetEntity(handsComp.CurrentHand);

                AddEntity(actor, actor, ent, InventoryLocation.Equipment);
            }
        }

        public void HandleUnEquipItemToFloor(object sender, InventoryUnEquipItemToFloorEventArgs args)
        {
            var actor = args.Actor;
            var eqComp = actor.GetComponent<EquipmentComponent>(ComponentFamily.Equipment);
            var toUnEquip = args.Item;
            if (eqComp == null || toUnEquip == null || !eqComp.IsEquipped(toUnEquip))
            {
                return;
            }
            RemoveEntity(actor, actor, toUnEquip, InventoryLocation.Equipment);
        }

        public void HandleUnEquipItemToHand(object sender, InventoryUnEquipItemToHandEventArgs args)
        {
            var actor = args.Actor;
            var eqComp = actor.GetComponent<EquipmentComponent>(ComponentFamily.Equipment);
            var handComp = actor.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            var toUnEquip = args.Item;
            if (handComp == null || eqComp == null || toUnEquip == null || !eqComp.IsEquipped(toUnEquip))
            {
                return;
            }
            var hand = handComp.CurrentHand;
            if (handComp.IsEmpty(hand))
            {
                AddEntity(actor, actor, toUnEquip, hand);
            }
        }

        public void HandleUnEquipItemToSpecifiedHand(object sender, InventoryUnEquipItemToSpecifiedHandEventArgs args)
        {
            var actor = args.Actor;
            var eqComp = actor.GetComponent<EquipmentComponent>(ComponentFamily.Equipment);
            var handComp = actor.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            var toUnEquip = args.Item;
            var hand = args.Hand;
            if (handComp == null || eqComp == null || toUnEquip == null || !handComp.IsEmpty(hand) || !eqComp.IsEquipped(toUnEquip))
            {
                return;
            }
            AddEntity(actor, actor, toUnEquip, hand);
        }

        public void HandleAddItemToInventory(object sender, InventoryAddItemToInventoryEventArgs args)
        {
            var actor = args.Actor;
            var item = args.Item;
            if (actor != null && item != null)
                AddEntity(actor, actor, item, InventoryLocation.Inventory);
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

            if (location == InventoryLocation.Any)
            {
                return RemoveEntity(user, inventory, toRemove, GetEntityLocationInEntity(inventory, toRemove));
            }

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
                    // TODO Find a better way
                    FreeMovementAndSprite(toRemove);
                    toRemove.SendMessage(this, ComponentMessageType.Dropped);
                    return true;
                }
            }
            else if ((location == InventoryLocation.Equipment) && comEquip != null)
            {
                if (comEquip.RemoveEntity(user, toRemove))
                {
                    //Do sprite stuff and detaching
                    EquippableComponent eqCompo = toRemove.GetComponent<EquippableComponent>(ComponentFamily.Equippable);
                    if(eqCompo != null) eqCompo.currentWearer = null;
                    FreeMovementAndSprite(toRemove);
                    // TODO Find a better way
                    toRemove.SendMessage(this, ComponentMessageType.ItemUnEquipped);
                    return true;
                }
            }

            return false;
        }

        public void EnslaveMovementAndSprite(Entity master, Entity slave)
        {
            slave.RemoveComponent(ComponentFamily.Mover);
            slave.AddComponent(ComponentFamily.Mover, EntityManager.ComponentFactory.GetComponent<SlaveMoverComponent>());
            slave.GetComponent<SlaveMoverComponent>(ComponentFamily.Mover).Attach(master);
            if (slave.HasComponent(ComponentFamily.Renderable) && master.HasComponent(ComponentFamily.Renderable))
            {
                slave.GetComponent<IRenderableComponent>(ComponentFamily.Renderable).SetMaster(master);
            }
        }

        public void FreeMovementAndSprite(Entity slave)
        {
            var toRemoveSlaveMover = slave.GetComponent<SlaveMoverComponent>(ComponentFamily.Mover);
            if (toRemoveSlaveMover != null)
            {
                toRemoveSlaveMover.Detach();
            }

            if (slave.HasComponent(ComponentFamily.Renderable))
            {
                slave.GetComponent<IRenderableComponent>(ComponentFamily.Renderable).UnsetMaster();
            }
            slave.RemoveComponent(ComponentFamily.Mover);
            slave.AddComponent(ComponentFamily.Mover, EntityManager.ComponentFactory.GetComponent<BasicMoverComponent>());
            slave.GetComponent<BasicItemComponent>(ComponentFamily.Item).HandleDropped();
        }

        public void HideEntity(Entity toHide)
        {
            var renderable = toHide.GetComponent<IRenderableComponent>(ComponentFamily.Renderable);
            renderable.Visible = false;
        }

        public void ShowEntity(Entity toShow)
        {
            var renderable = toShow.GetComponent<IRenderableComponent>(ComponentFamily.Renderable);
            renderable.Visible = true;
        }

        public bool AddEntity(Entity user, Entity inventory, Entity toAdd, InventoryLocation location = InventoryLocation.Any)
        {
            if (EntityIsInEntity(inventory, toAdd))
            {
                RemoveEntity(user, inventory, toAdd, InventoryLocation.Any);
            }
            var comHands = inventory.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            var comEquip = inventory.GetComponent<EquipmentComponent>(ComponentFamily.Equipment);
            var comInv = inventory.GetComponent<InventoryComponent>(ComponentFamily.Inventory);

            if ((location == InventoryLocation.Inventory) && comInv != null)
            {
                if (comInv.CanAddEntity(user, toAdd))
                {
                    HideEntity(toAdd);
                    return comInv.AddEntity(user, toAdd);
                }
            }
            else if ((location == InventoryLocation.HandLeft || location == InventoryLocation.HandRight) && comHands != null)
            {
                if (comHands.CanAddEntity(user, toAdd, location))
                {
                    comHands.AddEntity(user, toAdd, location);
                    ShowEntity(toAdd);
                    //Do sprite stuff and attaching
                    EnslaveMovementAndSprite(inventory, toAdd);
                    toAdd.GetComponent<BasicItemComponent>(ComponentFamily.Item).HandlePickedUp(inventory, location);
                    // TODO Find a better way
                    toAdd.SendMessage(this, ComponentMessageType.PickedUp);
                    return true;
                }
            }
            else if ((location == InventoryLocation.Equipment || location == InventoryLocation.Any) && comEquip != null)
            {
                if (comEquip.CanAddEntity(user, toAdd))
                {
                    comEquip.AddEntity(user, toAdd);
                    ShowEntity(toAdd);
                    EnslaveMovementAndSprite(inventory, toAdd);
                    EquippableComponent eqCompo = toAdd.GetComponent<EquippableComponent>(ComponentFamily.Equippable);
                    eqCompo.currentWearer = user;
                    toAdd.SendMessage(this, ComponentMessageType.ItemEquipped);
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

        protected InventoryLocation GetEntityLocationInEntity(Entity who, Entity what)
        {
            if(IsEquipped(who, what))
            {
                return InventoryLocation.Equipment;
            }
            if (IsInInventory(who, what))
            {
                return InventoryLocation.Inventory;
            }
            if (IsInHands(who, what))
            {
                var handsCompo = who.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
                return handsCompo.GetHand(what);
            }
            return InventoryLocation.None;
        }

        protected bool EntityIsInEntity(Entity who, Entity what)
        {
            return IsInHands(who, what) || IsInInventory(who, what) || IsEquipped(who, what);
        }

        protected bool IsInHands(Entity who, Entity what)
        {
            var handsComp = who.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            if(handsComp == null)
                return false;
            return handsComp.IsInHand(what);
        }

        protected bool IsInInventory(Entity who, Entity what)
        {
            var inventoryComp = who.GetComponent<InventoryComponent>(ComponentFamily.Inventory);
            if (inventoryComp == null)
                return false;
            return inventoryComp.containsEntity(what);
        }

        protected bool IsEquipped(Entity who, Entity what)
        {
            var eqComp = who.GetComponent<EquipmentComponent>(ComponentFamily.Equipment);
            if (eqComp == null)
                return false;

            return eqComp.IsEquipped(what);
        }

        #endregion
    }
}
