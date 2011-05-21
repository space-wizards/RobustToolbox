using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS3D_shared;
using SS3d_server.Modules.Map;
using Lidgren.Network;

namespace SS3d_server.Atom
{
    public class AtomManager //SERVERSIDE
    {
        #region Vars
        private Map map;
        private SS3DNetserver netServer;

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

        }
        #endregion

        #region Network
        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            AtomMessage messageType = (AtomMessage)message.ReadByte();
            switch (messageType)
            {
                case AtomMessage.SpawnAtom:
                    HandleSpawnAtom(message);
                    break;
                case AtomMessage.DeleteAtom:
                    HandleDeleteAtom(message);
                    break;
                case AtomMessage.Passthrough:
                    PassMessage(message);
                    break;
                default:
                    break;
            }
        }

        private void PassMessage(NetIncomingMessage message)
        {
            ushort uid = message.ReadUInt16();

            var atom = atomDictionary[uid];
            atom.HandleNetworkMessage(message);
        }

        private void HandleDeleteAtom(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }

        private void HandleSpawnAtom(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
