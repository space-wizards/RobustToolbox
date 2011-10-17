using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;
using SS3D_shared;
using ServerServices;
using ServerInterfaces;

namespace SS3D_Server.Modules
{
    public class PlayerManager: IService
    {
        /* This class will manage connected player sessions. */
        public Dictionary<int, PlayerSession> playerSessions;

        public ServerServiceType ServiceType { get { return ServerServiceType.PlayerManager; } }

        public PlayerManager()
        {
            playerSessions = new Dictionary<int, PlayerSession>();
            //We can actually query this by client connection or whatever we want using linq

        }

        public void NewSession(NetConnection client)
        {
            PlayerSession session = new PlayerSession(client);
            playerSessions.Add(playerSessions.Values.Count + 1, session);
        }

        public void SpawnPlayerMob(PlayerSession s)
        {
            //Spawn the player's atom. There's probably a much better place to do this.
            Atom.Atom a = SS3DServer.Singleton.atomManager.SpawnAtom("Atom.Mob.Human");
            Atom.Mob.Human human = (Atom.Mob.Human) a;
            if (s.assignedJob != null)
            {
                foreach (SpawnEquipDefinition def in s.assignedJob.SpawnEquipment)
                {
                    Atom.Atom newItem = SS3DServer.Singleton.atomManager.SpawnAtom(def.ObjectType);
                    human.EquipItem(newItem.Uid, def.Location);
                }
            }
            s.AttachToAtom(a);
        }

        public PlayerSession GetSessionByConnection(NetConnection client)
        {
            var sessions =
                from s in playerSessions
                where s.Value.connectedClient == client
                select s.Value;

            return sessions.First(); // Should only be one session per client. Returns that session, in theory.
        }

        public PlayerSession GetSessionByIp(string ip)
        {
            var sessions =
                from s in playerSessions
                where s.Value.connectedClient.RemoteEndpoint.Address.ToString().Equals(ip) //This is kinda silly. Comparing strings. Bleh.
                select s.Value;

            return sessions.First(); // Should only be one session per client. Returns that session, in theory.
        }

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            // Pass message on to session
            PlayerSession s = GetSessionByConnection(message.SenderConnection);
            s.HandleNetworkMessage(message);
        }

        public void EndSession(NetConnection client)
        {
            // Ends the session.
            PlayerSession session = GetSessionByConnection(client);
            LogManager.Log(session.name + " disconnected.", LogLevel.Information);
            //Detach the atom and (dont)delete it.
            var a = session.attachedAtom;
            session.DetachFromAtom();
        }

        public void SendJoinGameToAll()
        {
            foreach( PlayerSession s in playerSessions.Values)
            {
                s.JoinGame();
            }
        }

    }
}
