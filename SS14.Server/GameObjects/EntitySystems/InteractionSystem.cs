using SS14.Server.GameObjects.Events;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.System;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Server.GameObjects.EntitySystems
{
    [IoCTarget]
    public class InteractionSystem : EntityEventHandlingSystem
    {
        public override void SubscribeEvents()
        {
            base.SubscribeEvents();
            EntityManager.SubscribeEvent<ClickedOnEntityEventArgs>(new EntityEventHandler<ClickedOnEntityEventArgs>(HandleClickEvent), this);
            EntityManager.SubscribeEvent<BoundKeyChangeEventArgs>(new EntityEventHandler<BoundKeyChangeEventArgs>(HandleBoundKeyChangeEvent), this);
        }

        public void HandleClickEvent(object sender, ClickedOnEntityEventArgs args)
        {
            IEntity user = EntityManager.GetEntity(args.Clicker);
            IEntity obj = EntityManager.GetEntity(args.Clicked);
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
                        EntityManager.RaiseEvent(sender, new InventoryDroppedItemEventArgs { Actor = user });
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

        public bool UserClickedEntity(IEntity user, IEntity obj, int mouseClickType)
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

        private bool DoHandsToActorInteraction(IEntity user, IEntity obj)
        {
            var hands = user.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            if (hands.IsEmpty(hands.CurrentHand))
            {
                return DoEmptyHandToActorInteraction(user, obj);
            }
            return DoApplyItemToActor(user, hands.GetEntity(hands.CurrentHand), obj);
        }

        private bool DoApplyItemToActor(IEntity user, IEntity entity, IEntity obj)
        {
            return DoApplyItem(user, entity, obj, InteractsWith.Actor);
        }

        private bool DoEmptyHandToActorInteraction(IEntity user, IEntity obj)
        {
            // TODO Implementation for this
            return true;
        }

        private bool DoHandsToLargeObjectInteraction(IEntity user, IEntity obj)
        {
            var hands = user.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            if (hands.IsEmpty(hands.CurrentHand))
            {
                return DoEmptyHandToLargeObjectInteraction(user, obj);
            }
            return DoApplyItemToLargeObject(user, hands.GetEntity(hands.CurrentHand), obj);
        }

        private bool DoApplyItemToLargeObject(IEntity user, IEntity entity, IEntity obj)
        {
            return DoApplyItem(user, entity, obj, InteractsWith.LargeObject);
        }

        private bool DoEmptyHandToLargeObjectInteraction(IEntity user, IEntity obj)
        {
            // Send a message to obj that it has been clicked by user.
            obj.SendMessage(user, ComponentMessageType.ReceiveEmptyHandToLargeObjectInteraction, new object[1] { user });
            return true;
        }

        private bool DoHandsToItemInteraction(IEntity user, IEntity obj)
        {
            var hands = user.GetComponent<HumanHandsComponent>(ComponentFamily.Hands);
            if (hands.IsEmpty(hands.CurrentHand))
            {
                return DoEmptyHandToItemInteraction(user, obj);
            }
            return DoApplyItemToItem(user, hands.GetEntity(hands.CurrentHand), obj);
        }

        private bool DoApplyItemToItem(IEntity user, IEntity entity, IEntity obj)
        {
            return DoApplyItem(user, entity, obj, InteractsWith.Item);
        }

        private bool DoApplyItem(IEntity user, IEntity item, IEntity target, InteractsWith interaction)
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
        private bool DoEmptyHandToItemInteraction(IEntity user, IEntity obj)
        {
            var itemComponent = obj.GetComponent<BasicItemComponent>(ComponentFamily.Item);
            if (itemComponent.CanBePickedUp)
            {
                EntityManager.RaiseEvent(this, new InventoryPickedUpItemEventArgs { Actor = user, Item = obj });
            }
            return true;
        }
    }
}
