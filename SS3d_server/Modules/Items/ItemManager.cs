using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using SS3D_shared;
using SS3D_shared.HelperClasses;

namespace SS3d_server.Modules.Items
{
    public class ItemManager
    {
        private Map.Map map;
        private SS3DNetserver netServer;
        private Mobs.MobManager mobManager;
        private Dictionary<ushort, Item> itemDict; // ItemID, Item
        private List<ushort> itemsToSend;
        ushort lastID = 0;
        private string itemAssemblyName;
        private DateTime lastItemUpdateSent = DateTime.Now;
        private double itemUpdateTime = 100;
        private float fallSpeed = 5.0f; // Just a constant now (not acceleration) for testing.


        public ItemManager(SS3DNetserver _netServer, Map.Map _map, Mobs.MobManager _mobManager)
        {
            netServer = _netServer;
            map = _map;
            mobManager = _mobManager;
            itemDict = new Dictionary<ushort, Item>();
            itemAssemblyName = typeof(Item).Assembly.ToString();
            itemsToSend = new List<ushort>();
        }

        public void Update()
        {
            TimeSpan updateSpan = netServer.time - lastItemUpdateSent;
            if (updateSpan.TotalMilliseconds > itemUpdateTime)
            {
                foreach (Item item in itemDict.Values)
                {
                    UpdateItemPosition(item.itemID);
                }
                foreach (ushort itemID in itemsToSend)
                {
                    SendItemUpdate(itemID);
                }
                itemsToSend.Clear();
                lastItemUpdateSent = netServer.time;
            }

        }

        private void UpdateItemPosition(ushort itemID)
        {
            Vector3 currentPos = itemDict[itemID].serverInfo.position;
            Point arrayPosition = map.GetTileArrayPositionFromWorldPosition(currentPos);
            double heightAboveTile = itemDict[itemID].serverInfo.position.Y - (map.GetHeightAboveTileAt(currentPos) + 1.0f);
            if (heightAboveTile > 1.0)
            {
                if (heightAboveTile > fallSpeed)
                {
                    itemDict[itemID].serverInfo.position.Y -= fallSpeed;
                    itemsToSend.Add(itemID);
                }
                else
                {
                    itemDict[itemID].serverInfo.position.Y -= heightAboveTile;
                    itemsToSend.Add(itemID);
                }
            }

        }

        public void HandleNetMessage(NetIncomingMessage message)
        {
            ItemMessage messageType = (ItemMessage)message.ReadByte();
            switch (messageType)
            {
                case ItemMessage.CreateItem:
                    HandleCreateItem(message);
                    break;
                case ItemMessage.ClickItem:
                    HandleClickItem(message);
                    break;
            }

        }

        private void HandleCreateItem(NetIncomingMessage message)
        {
            string name = message.ReadString();
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            float z = message.ReadFloat();

            Type type = Type.GetType(name + "," + itemAssemblyName);

            object newItem = Activator.CreateInstance(type);
            lastID++;

            itemDict[lastID] = (Item)newItem;
            itemDict[lastID].serverInfo.position = new Vector3(x, y, z);
            itemDict[lastID].itemID = lastID;

            SendCreateItem(lastID);
        }

        private void HandleClickItem(NetIncomingMessage message)
        {
            MobHand hand = (MobHand)message.ReadByte();
            ushort itemID = message.ReadUInt16();
            ushort mobID = netServer.clientList[message.SenderConnection].mobID;

            Vector3 Dist = itemDict[itemID].serverInfo.position -= mobManager.mobDict[mobID].serverInfo.position;


            // If we don't know about the itemID or mobID something fishy is going on, or the client has some fucked up
            // information so lets just discard this message.
            if (!itemDict.ContainsKey(itemID))
            {
                return;
            }
            if (!mobManager.mobDict.ContainsKey(mobID))
            {
                return;
            }

            Item item = itemDict[itemID];
            Mob mob = mobManager.mobDict[mobID];

            if (Dist.Magnitude <= 32)
            {
                if (item.holder == null)
                {
                    switch (hand)
                    {
                        case MobHand.LHand:
                            if (mob.leftHandItem == null)
                            {
                                item.holder = mob;
                                item.holderHand = hand;
                                mob.leftHandItem = item;
                                SendPickupItem(itemID, mobID, hand);
                            }
                            break;
                        case MobHand.RHand:
                            if (mob.rightHandItem == null)
                            {
                                item.holder = mob;
                                item.holderHand = hand;
                                mob.rightHandItem = item;
                                SendPickupItem(itemID, mobID, hand);
                            }
                            break;
                    }
                }
            }
        }

        public void CreateItem(Type type, Vector3 pos)
        {
            object newItem = Activator.CreateInstance(type);
            lastID++;

            itemDict[lastID] = (Item)newItem;
            itemDict[lastID].serverInfo.position = pos;
            itemDict[lastID].itemID = lastID;

            SendCreateItem(lastID);
        }

        // Message size ~288 bits (about 0.036kb).
        // This means on map load if we had 40 players, and 500 items,
        // to send every item to every player at round start would be
        // around 720kb in total.
        private void SendCreateItem(ushort itemID)
        {
            if (!itemDict.Keys.Contains(itemID))
            {
                return;
            }

            NetOutgoingMessage message = netServer.netServer.CreateMessage();

            message.Write((byte)NetMessage.ItemMessage);
            message.Write((byte)ItemMessage.CreateItem);

            message.Write(itemDict[itemID].GetType().FullName);
            message.Write(itemID);
            message.Write((float)itemDict[itemID].serverInfo.position.X);
            message.Write((float)itemDict[itemID].serverInfo.position.Y);
            message.Write((float)itemDict[itemID].serverInfo.position.Z);
            
            netServer.SendMessageToAll(message);
            Console.WriteLine("Item sent with ID: " + itemID);
        }

        private void SendCreateItem(ushort itemID, NetConnection netConnection)
        {
            if (!itemDict.Keys.Contains(itemID))
            {
                return;
            }

            NetOutgoingMessage message = netServer.netServer.CreateMessage();

            message.Write((byte)NetMessage.ItemMessage);
            message.Write((byte)ItemMessage.CreateItem);

            message.Write(itemDict[itemID].GetType().FullName);
            message.Write(itemID);
            message.Write((float)itemDict[itemID].serverInfo.position.X);
            message.Write((float)itemDict[itemID].serverInfo.position.Y);
            message.Write((float)itemDict[itemID].serverInfo.position.Z);

            
            netServer.SendMessageTo(message, netConnection);
            Console.WriteLine("Item sent with ID: " + itemID);
        }

        private void SendItemUpdate(ushort itemID)
        {
            if (!itemDict.Keys.Contains(itemID))
            {
                return;
            }
            Vector3 pos = itemDict[itemID].serverInfo.position;

            NetOutgoingMessage message = netServer.netServer.CreateMessage();
            message.Write((byte)NetMessage.ItemMessage);
            message.Write((byte)ItemMessage.InterpolationPacket);
            message.Write(itemID);
            message.Write((float)pos.X);
            message.Write((float)pos.Y);
            message.Write((float)pos.Z);
            netServer.SendMessageToAll(message);
        }

        private void SendPickupItem(ushort itemID, ushort mobID, MobHand hand)
        {
            NetOutgoingMessage message = netServer.netServer.CreateMessage();
            message.Write((byte)NetMessage.ItemMessage);
            message.Write((byte)ItemMessage.PickUpItem);
            message.Write((byte)hand);
            message.Write(mobID);
            message.Write(itemID);
            netServer.SendMessageToAll(message);
        }

        private void SendPickupItem(ushort itemID, ushort mobID, MobHand hand, NetConnection netConnection)
        {
            NetOutgoingMessage message = netServer.netServer.CreateMessage();
            message.Write((byte)NetMessage.ItemMessage);
            message.Write((byte)ItemMessage.PickUpItem);
            message.Write((byte)hand);
            message.Write(mobID);
            message.Write(itemID);
            netServer.SendMessageTo(message, netConnection);
        }

        // A new player is joining so lets send them everything we know!
        // Each module should probably have one of these.
        public void NewPlayer(NetConnection netConnection) 
        {
            foreach (Item item in itemDict.Values)
            {
                if (item == null)
                {
                    continue;
                }
                SendCreateItem(item.itemID, netConnection);
                if (item.holder != null)
                {
                    SendPickupItem(item.itemID, item.holder.mobID, item.holderHand, netConnection);
                }
            }
        }
    }
}
