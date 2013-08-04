using System.Collections.Generic;
using System.Linq;
using GameObject;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GameStates;
using SS13_Shared.ServerEnums;
using ServerInterfaces;
using ServerInterfaces.GOC;
using ServerInterfaces.Player;
using ServerServices.Log;

namespace ServerServices.Player
{
    public class PlayerManager : IPlayerManager
    {
        /* This class will manage connected player sessions. */
        public Dictionary<int, PlayerSession> playerSessions;
        public ISS13Server server;

        public PlayerManager()
        {
            playerSessions = new Dictionary<int, PlayerSession>();
            //We can actually query this by client connection or whatever we want using linq
        }

        #region IPlayerManager Members

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
            Entity a = server.EntityManager.SpawnEntity("HumanMob");
            Entity human = a;
            a.GetComponent<ITransformComponent>(ComponentFamily.Transform).TranslateTo(new Vector2(160, 160));
            if (s.assignedJob != null)
            {
                foreach (
                    Entity newItem in
                        s.assignedJob.SpawnEquipment.Select(def => server.EntityManager.SpawnEntity(def.ObjectType)))
                {
                    newItem.GetComponent<ITransformComponent>(ComponentFamily.Transform).TranslateTo(
                        human.GetComponent<ITransformComponent>(ComponentFamily.Transform).Position);
                    //This is not neccessary once the equipment component is built.
                    human.SendMessage(this, ComponentMessageType.EquipItem, newItem);
                }
            }
            s.AttachToEntity(a);
        }

        public IPlayerSession GetSessionByConnection(NetConnection client)
        {
            IEnumerable<PlayerSession> sessions =
                from s in playerSessions
                where s.Value.connectedClient == client
                select s.Value;

            return sessions.FirstOrDefault(); // Should only be one session per client. Returns that session, in theory.
        }

        public IPlayerSession GetSessionByIp(string ip)
        {
            IEnumerable<PlayerSession> sessions =
                from s in playerSessions
                where s.Value.connectedClient.RemoteEndPoint.Address.ToString().Equals(ip)
                //This is kinda silly. Comparing strings. Bleh.
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
            IPlayerSession session = GetSessionByConnection(client);
            LogManager.Log(session.name + " disconnected.", LogLevel.Information);
            //Detach the entity and (dont)delete it.
            session.OnDisconnect();
        }


        public void SendJoinGameToAll()
        {
            foreach (PlayerSession s in playerSessions.Values)
            {
                s.JoinGame();
            }
        }

        public void SendJoinLobbyToAll()
        {
            foreach (PlayerSession s in playerSessions.Values)
            {
                s.JoinLobby();
            }
        }

        public void DetachAll()
        {
            foreach (PlayerSession s in playerSessions.Values)
            {
                s.DetachFromEntity();
            }
        }

        public IPlayerSession[] GetPlayersInRange(Vector2 position, int range)
        {
            return
                playerSessions.Values.Where(
                    x =>
                    x.attachedEntity != null &&
                    (position - x.attachedEntity.GetComponent<ITransformComponent>(ComponentFamily.Transform).Position).
                        Magnitude < range).ToArray();
        }

        public IPlayerSession[] GetPlayersInLobby()
        {
            return
                playerSessions.Values.Where(
                    x => x.status == SessionStatus.InLobby).ToArray();
        }

        public IPlayerSession[] GetAllPlayers()
        {
            return playerSessions.Values.ToArray();
        }

        public List<PlayerState> GetPlayerStates()
        {
            return playerSessions.Values.Select(s => s.PlayerState).ToList();
        }

        #endregion
    }
}