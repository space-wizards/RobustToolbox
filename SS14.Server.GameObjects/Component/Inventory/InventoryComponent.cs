using Lidgren.Network;
using SS14.Server.Interfaces.GOC;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.GO.Component.Inventory;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SS14.Server.GameObjects
{
    public class InventoryComponent : Component, IInventoryComponent, IInventoryContainer
    {
        public InventoryComponent()
        {
            Family = ComponentFamily.Inventory;
            containedEntities = new List<Entity>();
        }

        #region IInventoryComponent Members

        public List<Entity> containedEntities { get; private set; }

        public int maxSlots { get; private set; }

        public bool containsEntity(Entity entity)
        {
            if (containedEntities.Contains(entity)) return true;
            else return false;
        }

        #endregion

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            switch ((ComponentMessageType) message.MessageParameters[0])
            {
                //TODO route these through the InventorySystem
                case ComponentMessageType.InventoryAdd:
                    Entity entAdd = Owner.EntityManager.GetEntity((int) message.MessageParameters[1]);
                    if (entAdd != null)
                        AddToInventory(entAdd);
                    break;

                case ComponentMessageType.InventoryRemove:
                    Entity entRemove = Owner.EntityManager.GetEntity((int) message.MessageParameters[1]);
                    if (entRemove != null)
                        RemoveFromInventory(entRemove);
                    break;
            }
        }

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type,
                                                             params object[] list)
        {
            ComponentReplyMessage reply = base.RecieveMessage(sender, type, list);

            if (sender == this)
                return ComponentReplyMessage.Empty;

            switch (type)
            {
                case ComponentMessageType.DisassociateEntity:
                    RemoveFromInventory((Entity) list[0]);
                    break;

                case ComponentMessageType.InventoryAdd:
                    AddToInventory((Entity) list[0]);
                    break;

                case ComponentMessageType.InventoryRemove:
                    RemoveFromInventory((Entity) list[0]);
                    break;

                case ComponentMessageType.InventorySetSize:
                    maxSlots = (int) list[0];
                    break;
            }

            return reply;
        }

        public bool containsEntity(string templatename)
        {
            if (containedEntities.Exists(x => x.Template.Name == templatename)) return true;
            else return false;
        }

        public override void HandleExtendedParameters(XElement extendedParameters)
        {
            foreach (XElement param in extendedParameters.Descendants("InventoryProperties"))
            {
                if (param.Attribute("inventorySize") != null)
                    maxSlots = int.Parse(param.Attribute("inventorySize").Value);

                //TODO: Add support for objects that are created inside inventories (Lockers, crates etc)
            }
        }

        //Adds item to inventory and dispatches hide message to sprite compo.
        private void AddToInventory(Entity entity)
        {
            if (!containedEntities.Contains(entity) && containedEntities.Count < maxSlots)
            {
                Entity holder = null;
                if (entity.HasComponent(ComponentFamily.Item))
                    holder = ((BasicItemComponent) entity.GetComponent(ComponentFamily.Item)).CurrentHolder;
                if (holder == null && entity.HasComponent(ComponentFamily.Equippable))
                    holder = ((EquippableComponent) entity.GetComponent(ComponentFamily.Equippable)).currentWearer;
                if (holder != null) holder.SendMessage(this, ComponentMessageType.DisassociateEntity, entity);
                else Owner.SendMessage(this, ComponentMessageType.DisassociateEntity, entity);

                containedEntities.Add(entity);

                HandleAdded(entity);
            }
        }

        //Removes item from inventory and dispatches unhide message to sprite compo.
        public bool RemoveFromInventory(Entity entity)
        {
            if (containedEntities.Contains(entity))
            {
                containedEntities.Remove(entity);
            }
            HandleRemoved(entity);
            return true;
        }

        private void HandleAdded(Entity entity)
        {
            entity.SendMessage(this, ComponentMessageType.PickedUp, Owner, Hand.None);
            entity.SendMessage(this, ComponentMessageType.SetVisible, false);
        }

        private void HandleRemoved(Entity entity)
        {
            entity.SendMessage(this, ComponentMessageType.Dropped);
            entity.SendMessage(this, ComponentMessageType.SetVisible, true);
        }

        public bool RemoveEntity(Entity actor, Entity toRemove, InventoryLocation location = InventoryLocation.Any)
        {
            if (containedEntities.Contains(toRemove))
            {
                containedEntities.Remove(toRemove);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool AddEntity(Entity actor, Entity toAdd, InventoryLocation location = InventoryLocation.Any)
        {
            if (containedEntities.Contains(toAdd))
            {
                return false;
            }
            else
            {
                containedEntities.Add(toAdd);
                return true;
            }
        }

        public IEnumerable<Entity> GetEntitiesInInventory()
        {
            return containedEntities;
        }

        public override ComponentState GetComponentState()
        {
            List<int> entities = containedEntities.Select(x => x.Uid).ToList();
            return new InventoryComponentState(maxSlots, entities);
        }
    }
}