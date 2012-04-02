using System.Collections.Generic;
using System.Linq;

using Lidgren.Network;
using SS13_Shared;
using ServerInterfaces;
using ServerInterfaces.GameObject;
using ServerServices.Log;
using ServerInterfaces.Player;

namespace ServerServices.Player
{
    public class PlayerManager: IPlayerManager
    {
        /* This class will manage connected player sessions. */
        public Dictionary<int, PlayerSession> playerSessions;
        public ISS13Server server;

        public PlayerManager()
        {
            playerSessions = new Dictionary<int, PlayerSession>();
            //We can actually query this by client connection or whatever we want using linq
        }

        public void Initialize(ISS13Server _server)
        {
            server = _server;
        }

        public void NewSession(NetConnection client)
        {
            var session = new PlayerSession(client, this);
            playerSessions.Add(playerSessions.Values.Count + 1, session);
        }

        public void SpawnPlayerMob(IPlayerSession s)
        {
            //Spawn the player's entity. There's probably a much better place to do this.
            IEntity a = server.EntityManager.SpawnEntity("HumanMob");
            var human = a;
            a.Translate(new Vector2(160, 160));
            if (s.assignedJob != null)
            {
                foreach (var newItem in s.assignedJob.SpawnEquipment.Select(def => server.EntityManager.SpawnEntity(def.ObjectType)))
                {
                    newItem.Translate(human.Position); //This is not neccessary once the equipment component is built.
                    human.SendMessage(this, SS13_Shared.GO.ComponentMessageType.EquipItem, newItem);
                }
            }
            s.AttachToEntity(a);
        }

        public IPlayerSession GetSessionByConnection(NetConnection client)
        {
            var sessions =
                from s in playerSessions
                where s.Value.connectedClient == client
                select s.Value;

            return sessions.First(); // Should only be one session per client. Returns that session, in theory.
        }

        public IPlayerSession GetSessionByIp(string ip)
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
            IPlayerSession s = GetSessionByConnection(message.SenderConnection);
            s.HandleNetworkMessage(message);
        }

        public void EndSession(NetConnection client)
        {
            // Ends the session.
            var session = GetSessionByConnection(client);
            LogManager.Log(session.name + " disconnected.", LogLevel.Information);
            //Detach the entity and (dont)delete it.
            var a = session.attachedEntity;
            session.DetachFromEntity();
        }

        public void SendJoinGameToAll()
        {
            foreach (var s in playerSessions.Values)
            {
                s.JoinGame();
            }
        }

        public void SendJoinLobbyToAll()
        {
            foreach(var s in playerSessions.Values)
            {
                s.JoinLobby();
            }
        }

    }
}
