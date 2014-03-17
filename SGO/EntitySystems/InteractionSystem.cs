using System;
using GameObject;
using GameObject.System;
using Lidgren.Network;
using SGO.Events;
using SS13_Shared;
using SS13_Shared.GO;

namespace SGO.EntitySystems
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
            UserClickedEntity(user, obj);
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

        public bool UserClickedEntity(Entity user, Entity obj)
        {
            if (user.HasComponent(ComponentFamily.Hands))
            {
                //It's something with hands!
                if (obj.HasComponent(ComponentFamily.Item))
                {
                    //It's something with hands using their hands on an item!
                    return doHandsToItemInteraction(user, obj);
                }
                if (obj.HasComponent(ComponentFamily.LargeObject))
                {
                    //It's something with hands using their hands on a large object!
                    return doHandsToLargeObjectInteraction(user, obj);
                }
                if (obj.HasComponent(ComponentFamily.Actor))
                {
                    //It's something with hands using their hands on an actor!
                    return doHandsToActorInteraction(user, obj);
                }
            }
            return false;
        }

        private bool doHandsToActorInteraction(Entity user, Entity obj)
        {
            var hands = user.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            if (hands.IsEmpty(hands.CurrentHand))
            {
                return doEmptyHandToActorInteraction(user, obj);
            }
            return doApplyItemToActor(user, hands.GetEntity(hands.CurrentHand), obj);
        }

        private bool doApplyItemToActor(Entity user, Entity entity, Entity obj)
        {
            throw new NotImplementedException();
        }

        private bool doEmptyHandToActorInteraction(Entity user, Entity obj)
        {
            throw new NotImplementedException();
        }

        private bool doHandsToLargeObjectInteraction(Entity user, Entity obj)
        {
            var hands = user.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            if (hands.IsEmpty(hands.CurrentHand))
            {
                return doEmptyHandToLargeObjectInteraction(user, obj);
            }
            return doApplyItemToLargeObject(user, hands.GetEntity(hands.CurrentHand), obj);
        }

        private bool doApplyItemToLargeObject(Entity user, Entity entity, Entity obj)
        {
            throw new NotImplementedException();
        }

        private bool doEmptyHandToLargeObjectInteraction(Entity user, Entity obj)
        {
            throw new NotImplementedException();
        }

        private bool doHandsToItemInteraction(Entity user, Entity obj)
        {
            var hands = user.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            if (hands.IsEmpty(hands.CurrentHand))
            {
                return doEmptyHandToItemInteraction(user, obj);
            }
            return doApplyItemToItem(user, hands.GetEntity(hands.CurrentHand), obj);
        }

        private bool doApplyItemToItem(Entity user, Entity entity, Entity obj)
        {
            throw new NotImplementedException();
        }

        private bool doEmptyHandToItemInteraction(Entity user, Entity obj)
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