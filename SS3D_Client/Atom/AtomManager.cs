using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS3D_shared;
using SS3D.States;
using Lidgren.Network;

namespace SS3D.Atom
{
    public class AtomManager // CLIENTSIDE
    {
        #region Vars
        private GameScreen gameState;
        public Dictionary<ushort, Atom> atomDictionary;
        #endregion

        #region Instatiation
        public AtomManager(GameScreen _gameState)
        {
            gameState = _gameState;
        }
        #endregion

        #region Updating
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
                    //TODO: Get the atom id
                    //Pass the rest of the message through to the specific item in question. 
                    break;
                default:
                    break;
            }
        }

        private void HandleSpawnAtom(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }

        private void HandleDeleteAtom(NetIncomingMessage message)
        {
            ushort uid = message.ReadUInt16();
            throw new NotImplementedException();
        }

        private void PassMessage(NetIncomingMessage message)
        {
            ushort uid = message.ReadUInt16();

            var atom = atomDictionary[uid];
            atom.HandleNetworkMessage(message);
        }
        #endregion
    }
}
