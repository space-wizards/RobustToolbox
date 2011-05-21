using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using SS3D_shared;
using SS3d_server.Modules.Map;
using Lidgren.Network;

namespace SS3d_server.Atom
{
    public class AtomManager //SERVERSIDE
    {
        #region Vars
        private Map map;
        public SS3DNetserver netServer;

        public Dictionary<ushort, Atom> atomDictionary;
        #endregion

        #region instantiation
        public AtomManager(SS3DNetserver _netServer)
        {
            netServer = _netServer;
        }
        #endregion

        #region updating
        public void Update()
        {
            // Using LINQ to find atoms that have flagged themselves as needing an update.
            // TODO: Modify to add an update queue that will run updates for atoms that need it on a time schedule
            var updateList =
                from atom in atomDictionary
                where atom.Value.updateRequired == true
                select atom.Value;
           
            //Update all of the bastards in the update list
            foreach (Atom a in updateList)
            {
                a.Update();
            }
        }
        #endregion

        #region Network
        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            AtomManagerMessage messageType = (AtomManagerMessage)message.ReadByte();
            switch (messageType)
            {
                case AtomManagerMessage.Passthrough:
                    // Pass a message to the atom in question
                    PassMessage(message);
                    break;
                default:
                    break;
            }
        }

        #region atom synchronization
        private void PassMessage(NetIncomingMessage message)
        {
            // Get atom id
            ushort uid = message.ReadUInt16();

            var atom = atomDictionary[uid];
            // Pass the message
            atom.HandleNetworkMessage(message);
        }

        private void SendMessage(ushort uid, NetOutgoingMessage message)
        {
            //Message should already have the uid attached by the atom in question. Just need to send it here.
        }
        #endregion

        #region deletion
        private void SendDeleteAtom(ushort uid)
        {
            NetOutgoingMessage message = netServer.netServer.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.DeleteAtom);
            message.Write(uid);
            netServer.SendMessageToAll(message);
        }
        #endregion

        #region spawning
        private void SendSpawnAtom(ushort uid, string type)
        {
            NetOutgoingMessage message = netServer.netServer.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.SpawnAtom);
            message.Write(uid);
            message.Write(type);
            netServer.SendMessageToAll(message);
        }

        public Atom SpawnAtom(string type)
        {
            ushort uid = (ushort)(1 + atomDictionary.Count);

            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            Type atomType = currentAssembly.GetType(type);
            object atom = Activator.CreateInstance(atomType); // Create atom of type atomType with parameters uid, this
            atomDictionary[uid] = (Atom)atom;

            atomDictionary[uid].uid = uid;
            atomDictionary[uid].atomManager = this;

            SendSpawnAtom(uid, type);

            return atomDictionary[uid]; // Why do we return it? So we can do whatever is needed easily from the calling function.

        }
        #endregion

        #endregion

        public void DeleteAtom(ushort uid)
        {
            // Delete the atom and send a delete atom message
            atomDictionary.Remove(uid);
            SendDeleteAtom(uid);
        }
    }
}
