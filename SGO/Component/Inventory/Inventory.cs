using System.Collections.Generic;
using System.Xml.Linq;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using ServerInterfaces.GameObject;

namespace SGO
{
    public class InventoryComponent : GameObjectComponent,IInventoryComponent
    {
        public InventoryComponent()
        {
            family = ComponentFamily.Inventory;
            containedEntities = new List<IEntity>();
        }

        public List<IEntity> containedEntities { get; private set; }

        public int maxSlots { get; private set; }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection client)
        {
            switch ((ComponentMessageType) message.MessageParameters[0])
            {
                case ComponentMessageType.InventoryInformation:
                    SendFullListing(client);
                    break;

                case ComponentMessageType.InventoryAdd:
                    IEntity entAdd = EntityManager.Singleton.GetEntity((int) message.MessageParameters[1]);
                    if (entAdd != null)
                        AddToInventory(entAdd);
                    break;

                case ComponentMessageType.InventoryRemove:
                    IEntity entRemove = EntityManager.Singleton.GetEntity((int) message.MessageParameters[1]);
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

                case ComponentMessageType.InventoryInformation:
                    reply = new ComponentReplyMessage(ComponentMessageType.InventoryInformation, maxSlots,
                                                      containedEntities);
                    break;

                case ComponentMessageType.InventorySetSize:
                    maxSlots = (int) list[0];
                    break;
            }

            return reply;
        }

        public bool containsEntity(IEntity entity)
        {
            if (containedEntities.Contains(entity)) return true;
            else return false;
        }

        public bool containsEntity(string templatename)
        {
            if (containedEntities.Exists(x => x.Template.Name == templatename)) return true;
            else return false;
        }

        public override void HandleInstantiationMessage(NetConnection netConnection)
        {
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

        private void SendFullListing(NetConnection connection)
        {
            var objArray = new object[containedEntities.Count + 3];

            objArray[0] = ComponentMessageType.InventoryInformation;
            objArray[1] = maxSlots;
            objArray[2] = containedEntities.Count;

            for (int i = 0; i < containedEntities.Count; i++)
            {
                objArray[3 + i] = containedEntities[i].Uid;
            }

            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, connection, objArray);
        }

        //Adds item to inventory and dispatches hide message to sprite compo.
        private void AddToInventory(IEntity entity)
        {
            if (!containedEntities.Contains(entity) && containedEntities.Count < maxSlots)
            {
                Entity holder = null;
                if (entity.HasComponent(ComponentFamily.Item))
                    holder = ((BasicItemComponent) entity.GetComponent(ComponentFamily.Item)).currentHolder;
                if (holder == null && entity.HasComponent(ComponentFamily.Equippable))
                    holder = ((EquippableComponent) entity.GetComponent(ComponentFamily.Equippable)).currentWearer;
                if (holder != null) holder.SendMessage(this, ComponentMessageType.DisassociateEntity, entity);
                else Owner.SendMessage(this, ComponentMessageType.DisassociateEntity, entity);

                containedEntities.Add(entity);

                HandleAdded(entity);
                Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null,
                                                  ComponentMessageType.InventoryUpdateRequired);
            }
        }

        //Removes item from inventory and dispatches unhide message to sprite compo.
        private void RemoveFromInventory(IEntity entity)
        {
            if (containedEntities.Contains(entity))
            {
                containedEntities.Remove(entity);
            }
            HandleRemoved(entity);
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, null,
                                              ComponentMessageType.InventoryUpdateRequired);
        }

        private void HandleAdded(IEntity entity)
        {
            entity.SendMessage(this, ComponentMessageType.PickedUp, Owner, Hand.None);
            entity.SendMessage(this, ComponentMessageType.SetVisible, false);
        }

        private void HandleRemoved(IEntity entity)
        {
            entity.SendMessage(this, ComponentMessageType.Dropped);
            entity.SendMessage(this, ComponentMessageType.SetVisible, true);
        }
    }
}