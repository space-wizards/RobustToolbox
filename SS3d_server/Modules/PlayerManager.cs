using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lidgren.Network;

namespace SS3d_server.Modules
{
    public class PlayerManager
    {
        /* This class will manage connected player sessions. */
        public Dictionary<int, PlayerSession> playerSessions;
        public SS3DNetserver netServer;

        public PlayerManager(SS3DNetserver _netServer)
        {
            playerSessions = new Dictionary<int, PlayerSession>();
            //We can actually query this by client connection or whatever we want using linq

            netServer = _netServer;
        }

        public void NewSession(NetConnection client)
        {
            PlayerSession session = new PlayerSession(client, netServer);
            playerSessions.Add(playerSessions.Values.Count + 1, session);

            //Spawn the player's atom. There's probably a much better place to do this.
            Atom.Atom a = netServer.atomManager.SpawnAtom("Atom.Mob.Human");
            session.AttachToAtom(a);
        }

        public PlayerSession GetSessionByConnection(NetConnection client)
        {
            var sessions =
                from s in playerSessions
                where s.Value.connectedClient == client
                select s.Value;

            return sessions.First(); // Should only be one session per client. Returns that session, in theory.
        }

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            PlayerSessionMessage messageType = (PlayerSessionMessage)message.ReadByte();
            switch (messageType)
            {
                default:
                    break;
            }
        }
    }
}
