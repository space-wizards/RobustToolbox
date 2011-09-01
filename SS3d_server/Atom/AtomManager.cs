using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

using SS3D_shared;
using SS3D_shared.HelperClasses;
using SS3D_Server.Modules.Map;
using Lidgren.Network;
using SS3D_Server.Modules;

namespace SS3D_Server.Atom
{
    public class AtomManager //SERVERSIDE
    {
        #region Vars

        public Dictionary<ushort, Atom> atomDictionary;
        private ushort lastUID = 0;
        #endregion

        #region instantiation
        public AtomManager()
        {
            atomDictionary = new Dictionary<ushort, Atom>();
        }
        #endregion

        #region updating
        public void Update(float framePeriod)
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
                a.Update(framePeriod);
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
                case AtomManagerMessage.SpawnAtom:
                    string type = message.ReadString();
                    Vector2 position = new Vector2(message.ReadFloat(), message.ReadFloat());
                    float rotation = message.ReadFloat();
                    SpawnAtom(type, position, rotation);
                    break;
                case AtomManagerMessage.DeleteAtom:
                    ushort uid = message.ReadUInt16();
                    DeleteAtom(uid);
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

        public void NewPlayer(NetConnection connection)
        {
            SendAllAtoms(connection);
        }
        #endregion

        #region deletion
        private void SendDeleteAtom(ushort uid)
        {
            NetOutgoingMessage message = SS3DNetServer.Singleton.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.DeleteAtom);
            message.Write(uid);
            SS3DServer.Singleton.SendMessageToAll(message);
        }
        #endregion

        #region spawning
        private void SendSpawnAtom(ushort uid, string type)
        {
            Atom atom = atomDictionary[uid];
            NetOutgoingMessage message = SS3DNetServer.Singleton.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.SpawnAtom);
            message.Write(uid);
            message.Write(type);
            message.Write(atom.drawDepth);
            SS3DServer.Singleton.SendMessageToAll(message);
        }

        private void SendSpawnAtom(ushort uid, string type, NetConnection client)
        {
            Atom atom = atomDictionary[uid];
            NetOutgoingMessage message = SS3DNetServer.Singleton.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.SpawnAtom);
            message.Write(uid);
            message.Write(type);
            message.Write(atom.drawDepth);
            SS3DServer.Singleton.SendMessageTo(message, client);
        }

        public void SendAllAtoms(NetConnection client)
        {
            // Send all atoms to a specific client. 
            // TODO: Make this less fucking stupid ie: send all of the atoms in a condensed form instead of this heavy-handed looping shit.
            // This will get laggy later on.
            foreach(Atom atom in atomDictionary.Values) 
            {
                SendSpawnAtom(atom.uid, AtomName(atom), client);
            }
            ///Tell each atom to do its post-instantiation shit. Theoretically, this should all occur after each atom has
            ///been spawned and instantiated on the clientside. Network traffic wise this might be weird
            ///
            foreach (Atom atom in atomDictionary.Values)
            {
                atom.SendState(client);
            }
            
        }

        public string AtomName(object atom)
        {
            string type = atom.GetType().ToString();
            type = type.Substring(type.IndexOf(".") + 1); // Fuckugly method of stripping the assembly name of the type.

            return type;
        }

        public Atom SpawnAtom(string type)
        {
            ushort uid = lastUID++;

            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            Type atomType = currentAssembly.GetType("SS3D_Server." + type, true);
            object atom = Activator.CreateInstance(atomType); // Create atom of type atomType with parameters uid, this
            atomDictionary[uid] = (Atom)atom;
            
            atomDictionary[uid].SetUp(uid, this);

            SendSpawnAtom(uid, type);
            atomDictionary[uid].SendState();
            
            return atomDictionary[uid]; // Why do we return it? So we can do whatever is needed easily from the calling function.
        }

        public Atom SpawnAtom(string type, Vector2 position)
        {
            Atom spawned = SpawnAtom(type);
            spawned.Translate(position);
            return spawned;
        }
        public Atom SpawnAtom(string type, Vector2 position, float rotation)
        {
            Atom spawned = SpawnAtom(type);
            spawned.Translate(position, rotation);
            return spawned;
        }

        #endregion

        /// <summary>
        ///  <para>Broadcasts draw depth value of atom to all connected players.</para>
        /// </summary>
        private void SendAtomDrawDepth(ushort uid, int depth)
        {
            NetOutgoingMessage message = SS3DNetServer.Singleton.CreateMessage();
            message.Write((byte)NetMessage.AtomManagerMessage);
            message.Write((byte)AtomManagerMessage.SetDrawDepth);
            message.Write(uid);
            message.Write(depth);
            SS3DServer.Singleton.SendMessageToAll(message);
        }
        #endregion

        /// <summary>
        ///  <para>Sets draw depth value of serverside instance and broadcasts draw depth value of atom to all connected players.</para>
        /// </summary>
        public void SetDrawDepthAtom(ushort uid, int depth)
        {
            SetDrawDepthAtom(atomDictionary[uid], depth);
        }

        /// <summary>
        ///  <para>Sets draw depth value of serverside instance and broadcasts draw depth value of atom to all connected players.</para>
        /// </summary>
        public void SetDrawDepthAtom(Atom atom, int depth)
        {
            atom.drawDepth = depth;
            SendAtomDrawDepth(atom.uid, depth);
        }

        public void DeleteAtom(ushort uid)
        {
            // Delete the atom and send a delete atom message
            DeleteAtom(atomDictionary[uid]);
        }

        public void DeleteAtom(Atom atom)
        {
            atomDictionary.Remove(atom.uid);
            atom.Destruct();
            SendDeleteAtom(atom.uid);
        }

        public Atom GetAtom(ushort uid)
        {
            if (atomDictionary.Keys.Contains(uid))
                return atomDictionary[uid];
            else
                return null;
        }

        public void SaveAtoms()
        {
            Stream s = File.Open("atoms.ss13", FileMode.Create);
            BinaryFormatter f = new BinaryFormatter();

            Console.WriteLine("Writing atoms to file...");
            List<Atom> saveList = new List<Atom>();
            foreach (Atom a in atomDictionary.Values)
            {
                if (a.IsChildOfType(typeof(Mob.Mob)))
                    continue;
                saveList.Add(a);
                Console.Write(".");
            }
            f.Serialize(s, saveList);
            Console.Write(".");
            s.Close();
            Console.WriteLine("Done!");
        }

        public void LoadAtoms()
        {
            if (!File.Exists("atoms.ss13"))
            {
                Console.WriteLine("***** Cannot find file atoms.ss13. Map starting empty *****");
                return;
            }

            Stream s = new FileStream("atoms.ss13", FileMode.Open);
            BinaryFormatter f = new BinaryFormatter();
            List<Atom> o = (List<Atom>)f.Deserialize(s);
            foreach (Atom a in o)
            {
                a.SetUp(lastUID++, this);
                a.SerializedInit();
                atomDictionary.Add(a.uid, a);
            }
            s.Close();
        }
    }
}
