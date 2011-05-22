using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using SS3D_shared;
using Mogre;

namespace SS3D.Modules.Items
{
    public class ItemManager
    {
        private Map.Map map;
        private OgreManager mEngine;
        private Network.NetworkManager networkManager;
        private Mobs.MobManager mobManager;

        public Dictionary<ushort, Item> itemDict // ItemID, Item
        {
            get;
            private set;
        }

        private List<ushort> itemsToMove;
        private List<ushort> itemsToStop;
        private string itemAssemblyName;
        private DateTime lastItemUpdate = DateTime.Now;
        private double itemUpdateTime = 20;
        private double serverUpdateTime = 100;

        public ItemManager(OgreManager _mEngine, Map.Map _map, Network.NetworkManager _networkManager, Mobs.MobManager _mobManager)
        {
            mEngine = _mEngine;
            map = _map;
            networkManager = _networkManager;
            mobManager = _mobManager;
            itemDict = new Dictionary<ushort, Item>();
            itemAssemblyName = typeof(SS3D_shared.Item).Assembly.ToString();
            itemsToMove = new List<ushort>();
            itemsToStop = new List<ushort>();
        }

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            ItemMessage messageType = (ItemMessage)message.ReadByte();
            switch (messageType)
            {
                case ItemMessage.CreateItem:
                    HandleCreateItem(message);
                    break;
                case ItemMessage.InterpolationPacket:
                    HandleInterpolationPacket(message);
                    break;
                case ItemMessage.PickUpItem:
                    HandlePickupItem(message);
                    break;
                default:
                    break; 
            }
        }

        public void Update()
        {
             TimeSpan updateSpan = DateTime.Now - lastItemUpdate;
             if (updateSpan.TotalMilliseconds > itemUpdateTime)
             {
                 foreach (ushort itemID in itemsToMove)
                 {
                     UpdateItemPosition(itemID);
                 }
                 lastItemUpdate = DateTime.Now;
                 foreach (ushort itemId in itemsToStop)
                 {
                     itemsToMove.Remove(itemId);
                 }
                 itemsToStop.Clear();
             }
        }

        public void Shutdown()
        {
            foreach (Item item in itemDict.Values)
            {
                //mEngine.SceneMgr.DestroyEntity(item.Entity);
                mEngine.SceneMgr.DestroySceneNode(item.Node);
            }
            itemDict = null;
            itemsToMove = null;
            itemsToStop = null;
        }

        #region Item moving
        private void HandleInterpolationPacket(NetIncomingMessage msg)
        {
            ushort itemID = msg.ReadUInt16();
            float x = msg.ReadFloat();
            float y = msg.ReadFloat();
            float z = msg.ReadFloat();
            SS3D_shared.HelperClasses.InterpolationPacket intPacket = new SS3D_shared.HelperClasses.InterpolationPacket(x,y,z,0,0,0);
            itemDict[itemID].interpolationPacket.Add(intPacket);

            if (itemDict[itemID].interpolationPacket.Count > 2)
            {
                itemDict[itemID].interpolationPacket.RemoveAt(0);
            }
            itemDict[itemID].lastUpdate = DateTime.Now;
            if(!itemsToMove.Contains(itemID))
            {
                itemsToMove.Add(itemID);
            }
        }

        private void UpdateItemPosition(ushort itemID)
        {
            TimeSpan timeSinceUpdate = DateTime.Now - itemDict[itemID].lastUpdate;
            if (timeSinceUpdate.TotalMilliseconds > serverUpdateTime*4 )
            {
                itemsToStop.Add(itemID);
                return;
            }
            Vector3 difference;
            if (itemDict[itemID].interpolationPacket.Count < 2)
            {

                difference = itemDict[itemID].interpolationPacket[0].position - itemDict[itemID].Node.Position;
            }
            else
            {
                difference = itemDict[itemID].interpolationPacket[1].position - itemDict[itemID].interpolationPacket[0].position;
            }
            difference /= (float)(serverUpdateTime / itemUpdateTime);
            itemDict[itemID].Node.Position += difference;
        }
        #endregion

        #region Item creation
        private void HandleCreateItem(NetIncomingMessage msg)
        {
            string itemName = msg.ReadString();
            ushort itemID = msg.ReadUInt16();
            float x = msg.ReadFloat();
            float y = msg.ReadFloat();
            float z = msg.ReadFloat();

            Type itemType = Type.GetType(itemName + "," + itemAssemblyName);
            object[] args = new object[3];
            args[0] = mEngine.SceneMgr;
            args[1] = new Vector3(x, y, z);
            args[2] = itemID;

            object newItem = Activator.CreateInstance(itemType, args);
            itemDict[itemID] = (SS3D_shared.Item)newItem;
        }

        public void SendCreateItem(Vector3 pos)
        {
            string name = typeof(Crowbar).FullName;
            NetOutgoingMessage message = networkManager.GetMessage();
            message.Write((byte)NetMessage.ItemMessage);
            message.Write((byte)ItemMessage.CreateItem);
            message.Write(name);
            message.Write(pos.x);
            message.Write(pos.y);
            message.Write(pos.z);

            mEngine.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
        }
        #endregion

        public void ClickItem(Item item)
        {
            NetOutgoingMessage message = networkManager.netClient.CreateMessage();
            message.Write((byte)NetMessage.ItemMessage);
            message.Write((byte)ItemMessage.ClickItem);
            message.Write(item.itemID);
            networkManager.SendMessage(message, NetDeliveryMethod.Unreliable);
        }

        private void HandlePickupItem(NetIncomingMessage message)
        {
            ushort mobID = message.ReadUInt16();
            ushort itemID = message.ReadUInt16();

            itemDict[itemID].Entity.DetachFromParent();
            Mob mob = mobManager.GetMob(mobID);

            if (mob == null)
            {
                return;
            }

            mob.Entity.AttachObjectToBone("RHand", itemDict[itemID].Entity);

            mob.heldItem = itemDict[itemID];
            itemDict[itemID].holder = mob;
            
        }
    }
}
