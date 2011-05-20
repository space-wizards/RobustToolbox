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
        private GameScreen gameState;
        public AtomManager(GameScreen _gameState)
        {
            gameState = _gameState;
        }
        public List<Atom> atomList;
        
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

        private void HandleSpawnAtom(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }

        private void HandleDeleteAtom(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }

        private void PassMessage(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }
    }
}
