using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using SS3D_shared;
using SS3D_shared.HelperClasses;


namespace SS3d_server.Modules.Mobs
{
    public class MobManager
    {
        private Map.Map map;
        private SS3DNetserver netServer;
        private Dictionary<ushort, Mob> mobDict; // mobID, mob
        ushort lastID = 0;
        private string mobAssemblyName;
        private DateTime lastmobUpdateSent = DateTime.Now;
        private double mobUpdateTime = 50;
      
        public MobManager(SS3DNetserver _netServer, Map.Map _map)
        {
            netServer = _netServer;
            map = _map;
            mobDict = new Dictionary<ushort, Mob>();
            mobAssemblyName = typeof(Mob).Assembly.ToString();
        }

        public void Update()
        {
            TimeSpan updateSpan = DateTime.Now - lastmobUpdateSent;
            if (updateSpan.TotalMilliseconds > mobUpdateTime)
            {
                foreach (Mob mob in mobDict.Values)
                {
                    UpdatemobPosition(mob.mobID);
                    SendmobUpdate(mob.mobID);
                }

                lastmobUpdateSent = DateTime.Now;
            }

        }

        private void UpdatemobPosition(ushort mobID)
        {
            Vector3 currentPos = mobDict[mobID].serverInfo.position;
        }

        private void RecieveMobPosUpdate(NetIncomingMessage message)
        {
            ushort mobID = message.ReadUInt16();
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            float z = message.ReadFloat();
            float rotW = message.ReadFloat();
            float rotY = message.ReadFloat();

            mobDict[mobID].serverInfo.position.X = x;
            mobDict[mobID].serverInfo.position.Y = y;
            mobDict[mobID].serverInfo.position.Z = z;
            mobDict[mobID].serverInfo.rotW = rotW;
            mobDict[mobID].serverInfo.rotY = rotY;
        }

        public void HandleNetMessage(NetIncomingMessage message)
        {
            MobMessage messageType = (MobMessage)message.ReadByte();
            switch (messageType)
            {
                case(MobMessage.InterpolationPacket):
                    RecieveMobPosUpdate(message);
                    break;
            }

        }

        private void SendCreatemob(ushort mobID, NetConnection netConnection)
        {
            if (!mobDict.Keys.Contains(mobID))
            {
                return;
            }

            NetOutgoingMessage message = netServer.netServer.CreateMessage();

            message.Write((byte)NetMessage.MobMessage);
            message.Write((byte)MobMessage.CreateMob);

            message.Write(mobDict[mobID].name);
            message.Write(mobID);
            if (mobID == netServer.clientList[netConnection].mobID)
            {
                message.Write(true);
            }
            else
            {
                message.Write(false);
            }
            message.Write((float)mobDict[mobID].serverInfo.position.X);
            message.Write((float)mobDict[mobID].serverInfo.position.Y);
            message.Write((float)mobDict[mobID].serverInfo.position.Z);
            message.Write((float)mobDict[mobID].serverInfo.rotW);
            message.Write((float)mobDict[mobID].serverInfo.rotY);
           
            netServer.SendMessageTo(message, netConnection);
            Console.WriteLine("mob sent with ID: " + mobID);
        }

        private void SendmobUpdate(ushort mobID)
        {
            if (!mobDict.Keys.Contains(mobID))
            {
                return;
            }
            Vector3 pos = mobDict[mobID].serverInfo.position;

            NetOutgoingMessage message = netServer.netServer.CreateMessage();
            message.Write((byte)NetMessage.MobMessage);
            message.Write((byte)MobMessage.InterpolationPacket);
            message.Write(mobID);
            message.Write((float)pos.X);
            message.Write((float)pos.Y);
            message.Write((float)pos.Z);
            message.Write((float)mobDict[mobID].serverInfo.rotW);
            message.Write((float)mobDict[mobID].serverInfo.rotY);
            netServer.SendMessageToAll(message);
        }

        // A new player is joining so lets send them everything we know!
        // Each module should probably have one of these.
        public void NewPlayer(NetConnection netConnection) 
        {
            CreateNewPlayerMob(netConnection);
            foreach (Mob mob in mobDict.Values)
            {
                if (mob == null)
                {
                    continue;
                }
                SendCreatemob(mob.mobID, netConnection);
            }
            foreach(NetConnection conn in netServer.clientList.Keys)
            {
                SendCreatemob(lastID, conn);
            }
        }

        private void CreateNewPlayerMob(NetConnection netConnection)
        {
            lastID++;
            Player player = new Player();
            player.name = "Player" + lastID.ToString();
            player.mobID = lastID;
            player.serverInfo.position = new Vector3(160, 0, 160);

            mobDict[lastID] = player;
            netServer.clientList[netConnection].mobID = lastID;
        }

        public void DeletePlayer(NetConnection netConnection)
        {
            ushort mobID = netServer.clientList[netConnection].mobID;
            mobDict.Remove(mobID);                       
        }
    }
}
