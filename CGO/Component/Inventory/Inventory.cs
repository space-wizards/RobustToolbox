using System.Collections.Generic;
using System.Linq;
using GameObject;
using SS13_Shared;
using SS13_Shared.GO;
using Lidgren.Network;

namespace CGO
{
    public class InventoryComponent : Component
    {
        public List<Entity> ContainedEntities { get; private set; }

        public int MaxSlots { get; private set; }

        public delegate void InventoryComponentUpdateHandler(InventoryComponent sender, int maxSlots, List<Entity> entities);
        public event InventoryComponentUpdateHandler Changed;

        public delegate void InventoryUpdateRequiredHandler(InventoryComponent sender);
        public event InventoryUpdateRequiredHandler UpdateRequired;
        
        public InventoryComponent()
        {
            Family = ComponentFamily.Inventory;
            ContainedEntities = new List<Entity>();
        }

        public override void HandleNetworkMessage(IncomingEntityComponentMessage message, NetConnection sender)
        {
            switch ((ComponentMessageType)message.MessageParameters[0])
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
            MaxSlots = (int)msg.MessageParameters[1];

            ContainedEntities.Clear();

            for (int i = 0; i < (int)msg.MessageParameters[2]; i++)
            {
                var msgPos = 3 + i;
                var entity = Owner.EntityManager.GetEntity((int)msg.MessageParameters[msgPos]);
                if (entity != null)
                    ContainedEntities.Add(entity);
            }

            if (Changed != null) Changed(this, MaxSlots, ContainedEntities);
        }

        public bool ContainsEntity(Entity entity)
        {
            return ContainedEntities.Contains(entity);
        }

        public bool ContainsEntity(string templatename)
        {
            return ContainedEntities.Exists(x => x.Template.Name == templatename);
        }

        public Entity GetEntity(string templatename)
        {
            return ContainedEntities.Exists(x => x.Template.Name == templatename) ? ContainedEntities.First(x => x.Template.Name == templatename) : null;
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

        public override ComponentReplyMessage RecieveMessage(object sender, ComponentMessageType type, params object[] list)
        {
            var reply = base.RecieveMessage(sender, type, list);

            if (sender == this) //Don't listen to our own messages!
                return ComponentReplyMessage.Empty;

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

            return reply;
        }
    }
}
