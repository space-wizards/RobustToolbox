using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using SS3D_shared;
using Mogre;

namespace SS3D.Modules.Mobs
{
    public class MobManager
    {
        private Map.Map map;
        private OgreManager mEngine;
        private Network.NetworkManager networkManager;
        private Dictionary<ushort, Mob> mobDict; // MobID, Mob
        private string mobAssemblyName;
        private DateTime lastMobUpdate = DateTime.Now;
        private DateTime lastSentUpdate = DateTime.Now;
        private DateTime lastAnimUpdate = DateTime.Now;
        private double mobUpdateTime = 10;
        private double serverUpdateTime = 50;
        
        private ushort myMobID = 0;

        public MobManager(OgreManager _mEngine, Map.Map _map, Network.NetworkManager _networkManager)
        {
            mEngine = _mEngine;
            map = _map;
            networkManager = _networkManager;
            mobDict = new Dictionary<ushort, Mob>();
           mobAssemblyName = typeof(SS3D_shared.Item).Assembly.ToString();
        }

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            MobMessage messageType = (MobMessage)message.ReadByte();
            switch (messageType)
            {
                case MobMessage.CreateMob:
                    HandleCreateMob(message);
                    break;
                case MobMessage.InterpolationPacket:
                    HandleInterpolationPacket(message);
                    break;
                case MobMessage.DeleteMob:
                    HandleDeleteMob(message);
                    break;
                default:
                    break; 
            }
        }

        public void Update()
        {
            TimeSpan updateSpan = DateTime.Now - lastMobUpdate;
             if (updateSpan.TotalMilliseconds > mobUpdateTime)
             {
                 foreach (ushort mobID in mobDict.Keys)
                 {
                     if (mobID == myMobID)
                     {
                         if (mobDict[mobID].interpolationPacket.Count > 0)
                         {
                             UpdateMyMobPosition();
                         }
                     }
                     else if (mobDict[mobID].interpolationPacket.Count > 0)
                     {
                         UpdateMobPosition(mobID);
                     }
                 }
                 lastMobUpdate = DateTime.Now;
             }


             if (mobDict.ContainsKey(myMobID))
             {
                 TimeSpan myLastSpan = DateTime.Now - lastSentUpdate;
                 if (myLastSpan.TotalMilliseconds > serverUpdateTime)
                 {
                     NetOutgoingMessage message = networkManager.netClient.CreateMessage();
                     message.Write((byte)NetMessage.MobMessage);
                     message.Write((byte)MobMessage.InterpolationPacket);

                     message.Write((ushort)myMobID);
                     message.Write((float)mobDict[myMobID].Node.Position.x);
                     message.Write((float)mobDict[myMobID].Node.Position.y);
                     message.Write((float)mobDict[myMobID].Node.Position.z);
                     message.Write((float)mobDict[myMobID].Node.Orientation.w);
                     message.Write((float)mobDict[myMobID].Node.Orientation.y);
                     networkManager.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
                     lastSentUpdate = DateTime.Now;
                 }
             }

             TimeSpan animTime = lastAnimUpdate - DateTime.Now;
             foreach (ushort mobID in mobDict.Keys)
             {
                 mobDict[mobID].animState.AddTime((float)animTime.TotalMilliseconds / 1000f);
             }
             lastAnimUpdate = DateTime.Now;
        }

        // This is horrible I know, it will be changed!
        public void MoveMe(int i)
        {
            mobDict[myMobID].animState = mobDict[myMobID].Entity.GetAnimationState("Walk");
            mobDict[myMobID].animState.Enabled = true;
            mobDict[myMobID].animState.Loop = true;
            Mogre.Vector3 lastPosition = mobDict[myMobID].Node.Position;
            switch (i)
            {
                case 1:
                    mobDict[myMobID].Node.Translate(new Vector3(1, 0, 0), Node.TransformSpace.TS_LOCAL);
                    break;
                case 2:
                    mobDict[myMobID].Node.Rotate(Mogre.Vector3.UNIT_Y, Mogre.Math.DegreesToRadians(-2));
                    break;
                case 3:
                    mobDict[myMobID].Node.Rotate(Mogre.Vector3.UNIT_Y, Mogre.Math.DegreesToRadians(2));
                    break;
                case 4:
                    mobDict[myMobID].Node.Translate(new Vector3(-1, 0, 0), Node.TransformSpace.TS_LOCAL);
                    break;
                default:
                    break;
            }
            foreach (AxisAlignedBox box in map.GetSurroundingAABB(mobDict[myMobID].Node.Position))
            {
                if (mobDict[myMobID].Entity.GetWorldBoundingBox().Intersects(box))
                {
                    mobDict[myMobID].Node.Position = lastPosition;
                    return;
                }
            }

        }

        public void Shutdown()
        {
            foreach (Mob mob in mobDict.Values)
            {
                //mEngine.SceneMgr.DestroyEntity(mob.Entity);
                mEngine.SceneMgr.DestroySceneNode(mob.Node);
            }
            mobDict = null;
        }

        #region Mob moving
        private void HandleInterpolationPacket(NetIncomingMessage msg)
        {
            ushort mobID = msg.ReadUInt16();
            float x = msg.ReadFloat();
            float y = msg.ReadFloat();
            float z = msg.ReadFloat();
            float rotW = msg.ReadFloat();
            float rotY = msg.ReadFloat();
            SS3D_shared.HelperClasses.InterpolationPacket intPacket = new SS3D_shared.HelperClasses.InterpolationPacket(x, y, z, rotW, rotY, 0);
            if(!mobDict.ContainsKey(mobID))
            {
                return;
            }

            mobDict[mobID].interpolationPacket.Add(intPacket);

            if (mobDict[mobID].interpolationPacket.Count > 10)
            {
                mobDict[mobID].interpolationPacket.RemoveAt(0);
            }
        }

        private void UpdateMobPosition(ushort mobID)
        {
            Vector3 difference;
            float rotW, rotY;

            // Removing this for now as I fucked it up somehow - need to read more.
            /*if (mobDict[mobID].interpolationPacket.Count < 10)
            {*/
            difference = mobDict[mobID].interpolationPacket[0].position - mobDict[mobID].Node.Position;
            rotW = mobDict[mobID].interpolationPacket[0].rotW;
            rotY = mobDict[mobID].interpolationPacket[0].rotY;
            /*}
            else
            {
                difference = mobDict[mobID].interpolationPacket[9].position- mobDict[mobID].interpolationPacket[8].position;
            }*/
                      
            if (System.Math.Round(difference.Length) != 0)
            {
                mobDict[mobID].animState = mobDict[mobID].Entity.GetAnimationState("Walk");
                mobDict[mobID].animState.Loop = true;
                mobDict[mobID].animState.Enabled = true;
            }
            else
            {
                mobDict[mobID].animState = mobDict[mobID].Entity.GetAnimationState("Idle");
                mobDict[mobID].animState.Loop = true;
                mobDict[mobID].animState.Enabled = true;
            }
            difference /= (float)(serverUpdateTime / mobUpdateTime);
            mobDict[mobID].Node.Position += difference;
            mobDict[mobID].Node.SetOrientation(rotW, 0, rotY, 0);
        }

        private void UpdateMyMobPosition()
        {




        }
        #endregion

        #region Mob creation
        private void HandleCreateMob(NetIncomingMessage msg)
        {
            string mobName = msg.ReadString();
            ushort mobID = msg.ReadUInt16();
            bool myMob = msg.ReadBoolean();
            float x = msg.ReadFloat();
            float y = msg.ReadFloat();
            float z = msg.ReadFloat();
            float rotW = msg.ReadFloat();
            float rotY = msg.ReadFloat();


            Player mob = new Player(mEngine.SceneMgr, new Vector3(x, y, z), mobID);
            mobDict[mobID] = mob;
            mob.Node.SetOrientation(rotW, 0, rotY, 0);

            if (myMob)
            {
                myMobID = mobID;

                mEngine.Camera.DetachFromParent();
                mEngine.Camera.Position = new Vector3(-160, 240, 0);

                SceneNode camNode = mob.Node.CreateChildSceneNode();
                camNode.AttachObject(mEngine.Camera);
                mEngine.Camera.SetAutoTracking(true, camNode);
            }
        }
        #endregion

        #region Mob Deletion
        private void HandleDeleteMob(NetIncomingMessage msg)
        {
            string mobName = msg.ReadString();
            ushort mobID = msg.ReadUInt16();

            if (!mobDict.Keys.Contains(mobID))
                return;

            Player player = (Player)mobDict[mobID];
            player.Delete();
            mobDict.Remove(mobID);
        }
        #endregion


    }
}
