using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using SS3D_shared;
using SS3D.States;
using SS3D.Modules;
using Lidgren.Network;
using Mogre;
using SS3D.Modules.Network;

namespace SS3D.Atom
{
    public class AtomManager // CLIENTSIDE
    {
        #region Vars
        public GameScreen gameState;
        public OgreManager mEngine;
        public NetworkManager networkManager;

        public Dictionary<ushort, Atom> atomDictionary;
        #endregion

        #region Instatiation
        public AtomManager(GameScreen _gameState)
        {
            gameState = _gameState;
            mEngine = gameState.mEngine;
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
            AtomManagerMessage messageType = (AtomManagerMessage)message.ReadByte();
            
            switch (messageType)
            {
                case AtomManagerMessage.SpawnAtom:
                    HandleSpawnAtom(message);
                    break;
                case AtomManagerMessage.DeleteAtom:
                    HandleDeleteAtom(message); 
                    break;
                case AtomManagerMessage.Passthrough:
                    //Pass the rest of the message through to the specific item in question. 
                    PassMessage(message);
                    break;
                default:
                    break;
            }
        }

        private void HandleSpawnAtom(NetIncomingMessage message)
        {
            ushort uid = message.ReadUInt16();
            string type = message.ReadString();

            Atom a = SpawnAtom(uid, type);
            a.SendPullMessage();
        }
        
        public Atom SpawnAtom(ushort uid, string type)
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            Type atomType = currentAssembly.GetType(type);
            object atom = Activator.CreateInstance(atomType); // Create atom of type atomType with parameters uid, this
            atomDictionary[uid] = (Atom)atom;

            atomDictionary[uid].uid = uid;
            atomDictionary[uid].atomManager = this;

            return atomDictionary[uid]; // Why do we return it? So we can do whatever is needed easily from the calling function.
        }
        
        private void HandleDeleteAtom(NetIncomingMessage message)
        {
            ushort uid = message.ReadUInt16();
            atomDictionary.Remove(uid);
        }

        // Passes a message to a specific atom.
        private void PassMessage(NetIncomingMessage message)
        {
            // Get the atom id
            ushort uid = message.ReadUInt16();

            var atom = atomDictionary[uid];
            // Pass the message to the atom in question.
            atom.HandleNetworkMessage(message);
        }

        private void SendMessage(NetOutgoingMessage message)
        {

        }

        #endregion
    }
}
