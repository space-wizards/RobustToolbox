using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces;
using SS14.Client.Interfaces.Player;
using SS14.Client.Player.PostProcessing;
using SS14.Client.State.States;
using SS14.Shared;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;

namespace SS14.Client.Player
{
    public class PlayerManager : IPlayerManager
    {
        /* Here's the player controller. This will handle attaching GUIS and input to controllable things.
         * Why not just attach the inputs directly? It's messy! This makes the whole thing nicely encapsulated.
         * This class also communicates with the server to let the server control what entity it is attached to. */

        private readonly List<PostProcessingEffect> _effects = new List<PostProcessingEffect>();

        [Dependency]
        private readonly IClientNetManager _networkManager;

        private SessionStatus status = SessionStatus.Zombie;

        public event EventHandler<TypeEventArgs> RequestedStateSwitch;

        private IBaseClient _client;
        private Dictionary<int, PlayerSession> _sessions;
        public int PlayerCount => _sessions.Count;
        public int MaxPlayers { get; set; }
        public LocalPlayer LocalPlayer { get; private set; }

        public void Initialize()
        {
            _sessions = new Dictionary<int, PlayerSession>();

            _client = IoCManager.Resolve<IBaseClient>();
            var netMan = IoCManager.Resolve<IClientNetManager>();
        }

        public void Update(float frameTime)
        {
            foreach (var e in _effects.ToArray())
            {
                e.Update(frameTime);
            }
        }
        
        public void ApplyEffects(RenderImage image)
        {
            foreach (var e in _effects)
            {
                e.ProcessImage(image);
            }
        }

        public void ApplyPlayerStates(List<PlayerState> list)
        {
            var myState = list.FirstOrDefault(s => s.UniqueIdentifier == _networkManager.Peer.UniqueIdentifier);
            if (myState == null)
                return;
            if (myState.ControlledEntity != null &&
                (LocalPlayer.ControlledEntity == null ||
                 LocalPlayer.ControlledEntity != null && myState.ControlledEntity != LocalPlayer.ControlledEntity.Uid))
                LocalPlayer.AttachEntity(
                    IoCManager.Resolve<IEntityManager>().GetEntity((int) myState.ControlledEntity));

            if (status != myState.Status)
                SwitchState(myState.Status);
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
            var message = _networkManager.CreateMessage();
            message.Write((byte) NetMessages.PlayerSessionMessage);
            message.Write((byte) PlayerSessionMessage.Verb);
            message.Write(verb);
            message.Write(uid);
            _networkManager.ClientSendMessage(message, NetDeliveryMethod.ReliableOrdered);
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

        private void SwitchState(SessionStatus newStatus)
        {
            status = newStatus;
            if (status == SessionStatus.InLobby && RequestedStateSwitch != null)
            {
                RequestedStateSwitch(this, new TypeEventArgs(typeof(Lobby)));
                LocalPlayer.DetatchEntity();
            }
        }
    }
}
