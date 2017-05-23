using Lidgren.Network;
using SS14.Server.GameObjects.Events;
using SS14.Server.Interfaces.GOC;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.Inventory;
using SS14.Shared.IoC;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using YamlDotNet.RepresentationModel;

namespace SS14.Server.GameObjects
{
    [IoCTarget]
    [Component("Inventory")]
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
                        //AddToInventory(entAdd);
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
                    // TODO Refactor craftingmanager to access this component directly, not using a message
                    AddToInventory((Entity) list[0]);
                    break;

                case ComponentMessageType.InventorySetSize:
                    maxSlots = (int) list[0];
                    break;
            }

            return reply;
        }

        public bool containsEntity(string templatename)
        {
            return containedEntities.Exists(x => x.Prototype.Name == templatename);
        }

        public override void LoadParameters(YamlMappingNode mapping)
        {
            YamlNode node;
            if (mapping.Children.TryGetValue(new YamlScalarNode("size"), out node))
            {
                maxSlots = int.Parse(((YamlScalarNode)node).Value);
            }
            // TODO: Add support for objects that are created inside inventories (Lockers, crates etc)
        }

        //Adds item to inventory and dispatches hide message to sprite compo.
        // TODO this method should be renamed to reflect what it really does
        private void AddToInventory(Entity entity)
        {
            Owner.EntityManager.RaiseEvent(this, new InventoryAddItemToInventoryEventArgs
            {
                Actor = Owner,
                Item = entity
            });
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
            entity.SendMessage(this, ComponentMessageType.PickedUp, Owner, InventoryLocation.None);
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

        public bool CanAddEntity(Entity actor, Entity toAdd, InventoryLocation location = InventoryLocation.Any)
        {
            if (containedEntities.Contains(toAdd))
            {
                return false;
            }

            // Todo check if inventory is full
            return true;
        }

        public bool AddEntity(Entity actor, Entity toAdd, InventoryLocation location = InventoryLocation.Any)
        {
            if (!CanAddEntity(actor, toAdd, location))
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
