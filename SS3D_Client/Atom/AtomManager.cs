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

        #region Instantiation
        public AtomManager(GameScreen _gameState)
        {
            gameState = _gameState;
            mEngine = gameState.mEngine;
            networkManager = mEngine.mNetworkMgr;
            atomDictionary = new Dictionary<ushort, Atom>();
        }

        public void Shutdown()
        {
            atomDictionary.Clear(); // Dump all the atoms, we is gettin da fuck outta here bro
        }
        #endregion

        #region Updating
        public void Update()
        {
            var updateList =
                from atom in atomDictionary
                where atom.Value.updateRequired == true
                select atom.Value;

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
            // Tell the atom to pull its position data etc. from the server
            a.SendPullMessage();
        }
        
        public Atom SpawnAtom(ushort uid, string type)
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            Type atomType = currentAssembly.GetType("SS3D." + type);
            object atom = Activator.CreateInstance(atomType); // Create atom of type atomType with parameters uid, this
            atomDictionary[uid] = (Atom)atom;

            atomDictionary[uid].SetUp(uid, this);

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

            //TODO add some real error handling here. We shouldn't be getting bad messages like this, and if we are, it means we're doing something out of sequence.
            if (!atomDictionary.Keys.Contains(uid))
                return;

            var atom = atomDictionary[uid];
            
            // Pass the message to the atom in question.
            atom.HandleNetworkMessage(message);
        }

        private void SendMessage(NetOutgoingMessage message)
        {

        }
        #endregion

        public Atom GetAtom(ushort uid)
        {
            if(atomDictionary.Keys.Contains(uid))
                return atomDictionary[uid];
            return null;
        }
    }
}
