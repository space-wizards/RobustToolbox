using System.Collections.Generic;
using System.Linq;
using SS13_Shared.GO;
using Lidgren.Network;

namespace CGO
{
    public class InventoryComponent : GameObjectComponent
    {
        public List<Entity> containedEntities { get; private set; }

        public int maxSlots { get; private set; }

        public delegate void InventoryComponentUpdateHandler(InventoryComponent sender, int maxSlots, List<Entity> entities);
        public event InventoryComponentUpdateHandler Changed;

        public delegate void InventoryUpdateRequiredHandler(InventoryComponent sender);
        public event InventoryUpdateRequiredHandler UpdateRequired;

        public InventoryComponent()
        {
            family = ComponentFamily.Inventory;
            containedEntities = new List<Entity>();
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message)
        {
            switch ((ComponentMessageType)message.messageParameters[0])
            {
                case ComponentMessageType.InventoryInformation:
                    UnpackListing(message);
                    break;
                case ComponentMessageType.InventoryUpdateRequired:
                    if (UpdateRequired != null) UpdateRequired(this);
                    break;
            }
        }

        private void UnpackListing(IncomingEntityComponentMessage msg)
        {
            maxSlots = (int)msg.messageParameters[1];

            containedEntities.Clear();

            for (int i = 0; i < (int)msg.messageParameters[2]; i++)
            {
                int msgPos = 3 + i;
                Entity ent = EntityManager.Singleton.GetEntity((int)msg.messageParameters[msgPos]);
                if (ent != null)
                    containedEntities.Add(ent);
            }

            if (Changed != null) Changed(this, maxSlots, containedEntities);
        }

        public bool containsEntity(Entity entity)
        {
            if (containedEntities.Contains(entity)) return true;
            else return false;
        }

        public bool containsEntity(string templatename)
        {
            if (containedEntities.Exists(x => x.template.Name == templatename)) return true;
            else return false;
        }

        public Entity getEntity(string templatename)
        {
            if (containedEntities.Exists(x => x.template.Name == templatename)) return containedEntities.First(x => x.template.Name == templatename);
            else return null;
        }

        public void SendRequestListing()
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, ComponentMessageType.InventoryInformation);
        }

        public void SendInventoryAdd(Entity ent)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, ComponentMessageType.InventoryAdd, ent.Uid);
        }

        public void SendInventoryRemove(Entity ent)
        {
            Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, ComponentMessageType.InventoryRemove, ent.Uid);
        }

        public override void RecieveMessage(object sender, ComponentMessageType type, List<ComponentReplyMessage> replies, params object[] list)
        {
            switch (type)
            {
                case ComponentMessageType.InventoryInformation:
                    Owner.SendComponentNetworkMessage(this, NetDeliveryMethod.ReliableUnordered, ComponentMessageType.InventoryInformation);
                    break;

                case ComponentMessageType.InventoryAdd:
                    SendInventoryAdd((Entity)list[0]);
                    break;

                case ComponentMessageType.InventoryRemove:
                    SendInventoryRemove((Entity)list[0]);
                    break;
            }
        }
    }
}
