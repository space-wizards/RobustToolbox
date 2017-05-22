using SS14.Server.GameObjects.Events;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using System;

namespace SS14.Server.GameObjects.EntitySystems
{
    public class InteractionSystem : EntityEventHandlingSystem
    {
        public InteractionSystem(EntityManager em, EntitySystemManager esm)
            : base(em, esm)
        {}

        public override void SubscribeEvents()
        {
            base.SubscribeEvents();
            EntityManager.SubscribeEvent<ClickedOnEntityEventArgs>(new EntityEventHandler<ClickedOnEntityEventArgs>(HandleClickEvent), this);
            EntityManager.SubscribeEvent<BoundKeyChangeEventArgs>(new EntityEventHandler<BoundKeyChangeEventArgs>(HandleBoundKeyChangeEvent), this);
        }

        public void HandleClickEvent(object sender, ClickedOnEntityEventArgs args)
        {
            Entity user = EntityManager.GetEntity(args.Clicker);
            Entity obj = EntityManager.GetEntity(args.Clicked);
            UserClickedEntity(user, obj, args.MouseButton);
        }

        public void HandleBoundKeyChangeEvent(object sender, BoundKeyChangeEventArgs args)
        {
            var user = args.Actor;
            var keyFunction = args.KeyFunction;
            var keyState = args.KeyState;
            //If key was just released after being pressed
            if (keyState == BoundKeyState.Up)
            {
                switch (keyFunction)
                {
                    case BoundKeyFunctions.Drop:
                        EntityManager.RaiseEvent(sender, new InventoryDroppedItemEventArgs {Actor = user});
                        break;
                    case BoundKeyFunctions.SwitchHands:
                        if (user.HasComponent(ComponentFamily.Hands))
                        {
                            user.GetComponent<HumanHandsComponent>(ComponentFamily.Hands).SwitchHands();
                        }
                        break;
                }
            }
        }

        public bool UserClickedEntity(Entity user, Entity obj, int mouseClickType)
        {
            if (user.HasComponent(ComponentFamily.Hands))
            {
                //It's something with hands!
                if (mouseClickType == MouseClickType.Left)
                {
                    if (obj.HasComponent(ComponentFamily.Item))
                    {
                        //It's something with hands using their hands on an item!
                        return DoHandsToItemInteraction(user, obj);
                    }
                    if (obj.HasComponent(ComponentFamily.LargeObject))
                    {
                        //It's something with hands using their hands on a large object!
                        return DoHandsToLargeObjectInteraction(user, obj);
                    }
                    if (obj.HasComponent(ComponentFamily.Actor))
                    {
                        //It's something with hands using their hands on an actor!
                        return DoHandsToActorInteraction(user, obj);
                    }
                }
                if (mouseClickType == MouseClickType.Right)
                {
                    if (obj.HasComponent(ComponentFamily.Item))
                    {
                        //It's something with hands using their hands on an item!
                        return DoHandsToItemInteraction(user, obj);
                    }
                    if (obj.HasComponent(ComponentFamily.LargeObject))
                    {
                        //It's something with hands using their hands on a large object!
                        return DoHandsToLargeObjectInteraction(user, obj);
                    }
                    if (obj.HasComponent(ComponentFamily.Actor))
                    {
                        //It's something with hands using their hands on an actor!
                        return DoHandsToActorInteraction(user, obj);
                    }
                }
            }
            return false;
        }

        private bool DoHandsToActorInteraction(Entity user, Entity obj)
        {
            var hands = user.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            if (hands.IsEmpty(hands.CurrentHand))
            {
                return DoEmptyHandToActorInteraction(user, obj);
            }
            return DoApplyItemToActor(user, hands.GetEntity(hands.CurrentHand), obj);
        }

        private bool DoApplyItemToActor(Entity user, Entity entity, Entity obj)
        {
            return DoApplyItem(user, entity, obj, InteractsWith.Actor);
        }

        private bool DoEmptyHandToActorInteraction(Entity user, Entity obj)
        {
            // TODO Implementation for this
            return true;
        }

        private bool DoHandsToLargeObjectInteraction(Entity user, Entity obj)
        {
            var hands = user.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            if (hands.IsEmpty(hands.CurrentHand))
            {
                return DoEmptyHandToLargeObjectInteraction(user, obj);
            }
            return DoApplyItemToLargeObject(user, hands.GetEntity(hands.CurrentHand), obj);
        }

        private bool DoApplyItemToLargeObject(Entity user, Entity entity, Entity obj)
        {
            return DoApplyItem(user, entity, obj, InteractsWith.LargeObject);
        }

        private bool DoEmptyHandToLargeObjectInteraction(Entity user, Entity obj)
        {
            // Send a message to obj that it has been clicked by user.
            obj.SendMessage(user, ComponentMessageType.ReceiveEmptyHandToLargeObjectInteraction, new object[1] { user });
            return true;
        }

        private bool DoHandsToItemInteraction(Entity user, Entity obj)
        {
            var hands = user.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            if (hands.IsEmpty(hands.CurrentHand))
            {
                return DoEmptyHandToItemInteraction(user, obj);
            }
            return DoApplyItemToItem(user, hands.GetEntity(hands.CurrentHand), obj);
        }

        private bool DoApplyItemToItem(Entity user, Entity entity, Entity obj)
        {
            return DoApplyItem(user, entity, obj, InteractsWith.Item);
        }

        private bool DoApplyItem(Entity user, Entity item, Entity target, InteractsWith interaction)
        {
            if (item == target) //Can't apply item to itself!
                return false;
            var itemComponent = item.GetComponent<BasicItemComponent>(ComponentFamily.Item);
            if (itemComponent != null)
            {
                //TODO move this logic somewhere that makes more sense than in the component
                itemComponent.ApplyTo(target, interaction, user);
            }
            return true;
        }
        private bool DoEmptyHandToItemInteraction(Entity user, Entity obj)
        {
            var itemComponent = obj.GetComponent<BasicItemComponent>(ComponentFamily.Item);
            if(itemComponent.CanBePickedUp)
            {
                EntityManager.RaiseEvent(this, new InventoryPickedUpItemEventArgs{Actor = user, Item = obj});
            }
            return true;
        }
    }
}
