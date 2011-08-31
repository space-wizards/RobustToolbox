using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using SS3D_shared;
using SS3D.States;
using SS3D.Modules;
using Lidgren.Network;
using SS3D.Modules.Network;
using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace SS3D.Atom
{
    public class AtomManager // CLIENTSIDE
    {
        #region Vars
        public GameScreen gameState;
        public Program prg;
        public NetworkManager networkManager;

        public Dictionary<ushort, Atom> atomDictionary;
        public DateTime now;
        public DateTime lastUpdate;
        public int updateRateLimit = 200; //200 updates / second
        #endregion

        #region Instantiation
        public AtomManager(GameScreen _gameState, Program _prg)
        {
            prg = _prg;
            gameState = _gameState;
            networkManager = prg.mNetworkMgr;
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
            now = DateTime.Now;
            //Rate limit
            TimeSpan timeSinceLastUpdate = now - lastUpdate;
            if (timeSinceLastUpdate.TotalMilliseconds < 1000 / updateRateLimit)
                return;

            var updateList =
                from atom in atomDictionary
                where atom.Value.updateRequired == true
                select atom.Value;

            foreach (Atom a in updateList)
            {
                a.Update(timeSinceLastUpdate.TotalMilliseconds);
            }
            lastUpdate = now;
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
                case AtomManagerMessage.SetDrawDepth:
                    HandleDrawDepth(message);
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

        private void HandleDrawDepth(NetIncomingMessage message)
        {
            ushort uid = message.ReadUInt16();
            int depth = message.ReadVariableInt32();
            atomDictionary[uid].drawDepth = depth;
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

        public string GetSpriteName(Type type) //This is ugly but if you can think of a better way please implement it.
        {
            Atom atom = (Atom)Activator.CreateInstance(type);
            string strName = atom.spritename;
            atom = null;
            return strName;
        }

        public bool GetSnapToGrid(Type type)
        {
            Atom atom = (Atom)Activator.CreateInstance(type);
            bool snap = atom.snapTogrid;
            atom = null;
            return snap;
        }

        
        private void HandleDeleteAtom(NetIncomingMessage message)
        {
            ushort uid = message.ReadUInt16();
            atomDictionary[uid] = null;
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
