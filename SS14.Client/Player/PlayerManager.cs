using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lidgren.Network;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Player;
using SS14.Client.Player.PostProcessing;
using SS14.Shared;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;

namespace SS14.Client.Player
{
    public class PlayerManager : IPlayerManager
    {
        /* Here's the player controller. This will handle attaching GUIS and input to controllable things.
         * Why not just attach the inputs directly? It's messy! This makes the whole thing nicely encapsulated.
         * This class also communicates with the server to let the server control what entity it is attached to. */

        private readonly List<PostProcessingEffect> _effects = new List<PostProcessingEffect>();

        [Dependency]
        private readonly IClientNetManager _network;

        [Dependency]
        private readonly IBaseClient _client;

        private Dictionary<int, PlayerSession> _sessions;
        public int PlayerCount => _sessions.Count;
        public int MaxPlayers => _client.GameInfo.ServerMaxPlayers;
        public LocalPlayer LocalPlayer { get; private set; }
        public IEnumerable<PlayerSession> Sessions => _sessions.Values;
        public IReadOnlyDictionary<int, PlayerSession> SessionsDict => _sessions;
        public event EventHandler PlayerListUpdated;

        public void Initialize()
        {
            _sessions = new Dictionary<int, PlayerSession>();

            _network.RegisterNetMessage<MsgPlayerListReq>(MsgPlayerListReq.NAME, (int) MsgPlayerListReq.ID, message =>
                Logger.Error($"[SRV] Unhandled NetMessage type: {message.MsgId}"));

            _network.RegisterNetMessage<MsgPlayerList>(MsgPlayerList.NAME, (int) MsgPlayerList.ID, HandlePlayerList);
        }

        public void Startup(INetChannel channel)
        {
            LocalPlayer = new LocalPlayer();

            var msgList = _network.CreateNetMessage<MsgPlayerListReq>();
            // message is empty
            _network.ClientSendMessage(msgList, NetDeliveryMethod.ReliableOrdered);
        }

        public void Update(float frameTime)
        {
            foreach (var e in _effects.ToArray())
            {
                e.Update(frameTime);
            }
        }

        public void Shutdown()
        {
            LocalPlayer = null;
            _sessions.Clear();
        }

        public void Destroy() { }

        public void ApplyEffects(RenderImage image)
        {
            foreach (var e in _effects)
            {
                e.ProcessImage(image);
            }
        }

        public void ApplyPlayerStates(List<PlayerState> list)
        {
            Debug.Assert(LocalPlayer != null, "Call Startup()");

            var myState = list.FirstOrDefault(s => s.UniqueIdentifier == _network.Peer.UniqueIdentifier);
            if (myState == null)
                return;
            if (myState.ControlledEntity != null &&
                (LocalPlayer.ControlledEntity == null ||
                 LocalPlayer.ControlledEntity != null && myState.ControlledEntity != LocalPlayer.ControlledEntity.Uid))
                LocalPlayer.AttachEntity(
                    IoCManager.Resolve<IEntityManager>().GetEntity((int) myState.ControlledEntity));

            if (LocalPlayer.Session.Status != myState.Status)
                LocalPlayer.SwitchState(myState.Status);
        }

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (PlayerSessionMessage) message.ReadByte();
            switch (messageType)
            {
                case PlayerSessionMessage.AttachToEntity:
                    break;
                case PlayerSessionMessage.JoinLobby:
                    break;
                case PlayerSessionMessage.AddPostProcessingEffect:
                    var effectType = (PostProcessingEffectType) message.ReadInt32();
                    var duration = message.ReadFloat();
                    AddEffect(effectType, duration);
                    break;
            }
        }

        /// <summary>
        ///     Verb sender
        ///     If UID is 0, it means its a global verb.
        /// </summary>
        /// <param name="verb">the verb</param>
        /// <param name="uid">a target entity's Uid</param>
        public void SendVerb(string verb, int uid)
        {
            var message = _network.CreateMessage();
            message.Write((byte) NetMessages.PlayerSessionMessage);
            message.Write((byte) PlayerSessionMessage.Verb);
            message.Write(verb);
            message.Write(uid);
            _network.ClientSendMessage(message, NetDeliveryMethod.ReliableOrdered);
        }

        // Player list came in from the server.
        private void HandlePlayerList(NetMessage netMessage)
        {
            //update sessions with player info
            var msg = (MsgPlayerList) netMessage;

            // diff the sessions to the Plyers
            var sessions = Sessions.ToList(); // we modify the collection, so it must be cached
            foreach (var session in sessions)
            {
                // should these be mapped NetId -> PlyInfo?
                var info = msg.Plyrs.FirstOrDefault(plyr => plyr.NetId == session.NetID);

                // slot already occupied
                if (info != null)
                {
                    if (info.Uuid != session.Uuid) // not the same player
                    {
                        _sessions.Remove(info.NetId);
                        var newSession = new PlayerSession(this, info.NetId, info.Uuid);
                        newSession.Name = info.Name;
                        newSession.Status = (SessionStatus) info.Status;
                        newSession.Ping = info.Ping;
                        _sessions.Add(info.NetId, newSession);
                    }
                    else // same player, update info
                    {
                        session.Name = info.Name;
                        session.Status = (SessionStatus) info.Status;
                        session.Ping = info.Ping;
                    }
                }
                else // could not find it, which means it was removed.
                {
                    _sessions.Remove(session.NetID);
                }
            }

            //raise event
            PlayerListUpdated?.Invoke(this, EventArgs.Empty);
        }

        public void AddEffect(PostProcessingEffectType type, float duration)
        {
            PostProcessingEffect e;
            switch (type)
            {
                case PostProcessingEffectType.Blur:
                    e = new BlurPostProcessingEffect(duration);
                    e.OnExpired += EffectExpired;
                    _effects.Add(e);
                    break;
                case PostProcessingEffectType.Death:
                    e = new DeathPostProcessingEffect(duration);
                    e.OnExpired += EffectExpired;
                    _effects.Add(e);
                    break;
            }
        }

        private void EffectExpired(PostProcessingEffect effect)
        {
            effect.OnExpired -= EffectExpired;
            if (_effects.Contains(effect))
                _effects.Remove(effect);
        }
    }
}
