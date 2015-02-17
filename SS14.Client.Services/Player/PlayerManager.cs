using SS14.Client.Graphics.CluwneLib.Render;
using SFML.Window;
using Lidgren.Network;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Player;
using SS14.Client.Services.Player.PostProcessing;
using SS14.Client.Services.State.States;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameStates;
using SS14.Shared.GO;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.Services.Player
{
    public class PlayerManager : IPlayerManager
    {
        /* Here's the player controller. This will handle attaching GUIS and input to controllable things.
         * Why not just attach the inputs directly? It's messy! This makes the whole thing nicely encapsulated. 
         * This class also communicates with the server to let the server control what entity it is attached to. */

        private readonly List<PostProcessingEffect> _effects = new List<PostProcessingEffect>();
        private readonly INetworkManager _networkManager;
        private SessionStatus status = SessionStatus.Zombie;

        public PlayerManager(INetworkManager networkManager)
        {
            _networkManager = networkManager;
        }

        #region IPlayerManager Members

        public event EventHandler<TypeEventArgs> RequestedStateSwitch;
        public event EventHandler<VectorEventArgs> OnPlayerMove;

        public Entity ControlledEntity { get; private set; }

        public void Update(float frameTime)
        {
            foreach (PostProcessingEffect e in _effects.ToArray())
            {
                e.Update(frameTime);
            }
        }

        public void Attach(Entity newEntity)
        {
            ControlledEntity = newEntity;
            ControlledEntity.AddComponent(ComponentFamily.Input,
                                          IoCManager.Resolve<IEntityManagerContainer>().EntityManager.ComponentFactory.
                                              GetComponent("KeyBindingInputComponent"));
            ControlledEntity.AddComponent(ComponentFamily.Mover,
                                          IoCManager.Resolve<IEntityManagerContainer>().EntityManager.ComponentFactory.
                                              GetComponent("PlayerInputMoverComponent"));
            ControlledEntity.AddComponent(ComponentFamily.Collider,
                                          IoCManager.Resolve<IEntityManagerContainer>().EntityManager.ComponentFactory.
                                              GetComponent("ColliderComponent"));
            ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove += PlayerEntityMoved;
        }

        public void ApplyEffects(RenderImage image)
        {
            foreach (PostProcessingEffect e in _effects)
            {
                e.ProcessImage(image);
            }
        }

        public void Detach()
        {
            if (ControlledEntity != null && ControlledEntity.Initialized)
            {
                ControlledEntity.RemoveComponent(ComponentFamily.Input);
                ControlledEntity.RemoveComponent(ComponentFamily.Mover);
                ControlledEntity.RemoveComponent(ComponentFamily.Collider);
                var transform = ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform);
                if(transform != null)
                    transform.OnMove -= PlayerEntityMoved;
            }
            ControlledEntity = null;
        }

        public void KeyDown(KeyboardKeys key)
        {
        }

        public void KeyUp(KeyboardKeys key)
        {
        }

        public void ApplyPlayerStates(List<PlayerState> list)
        {
            PlayerState myState = list.FirstOrDefault(s => s.UniqueIdentifier == _networkManager.UniqueId);
            if (myState == null)
                return;
            if (myState.ControlledEntity != null &&
                (ControlledEntity == null ||
                 (ControlledEntity != null && myState.ControlledEntity != ControlledEntity.Uid)))
                Attach(
                    IoCManager.Resolve<IEntityManagerContainer>().EntityManager.GetEntity((int) myState.ControlledEntity));

            if (status != myState.Status)
                SwitchState(myState.Status);
        }

        #endregion

        #region netcode

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (PlayerSessionMessage) message.ReadByte();
            switch (messageType)
            {
                case PlayerSessionMessage.AttachToEntity:
                    //HandleAttachToEntity(message);
                    break;
                case PlayerSessionMessage.JoinLobby:
                    /*if (RequestedStateSwitch != null)
                    {
                        RequestedStateSwitch(this, new TypeEventArgs(typeof(LobbyScreen)));
                        Detach();
                    }*/
                    break;
                case PlayerSessionMessage.AddPostProcessingEffect:
                    var effectType = (PostProcessingEffectType) message.ReadInt32();
                    float duration = message.ReadFloat();
                    AddEffect(effectType, duration);
                    break;
            }
        }

        /// <summary>
        /// Verb sender
        /// If UID is 0, it means its a global verb.
        /// </summary>
        /// <param name="verb">the verb</param>
        /// <param name="uid">a target entity's Uid</param>
        public void SendVerb(string verb, int uid)
        {
            NetOutgoingMessage message = _networkManager.CreateMessage();
            message.Write((byte) NetMessage.PlayerSessionMessage);
            message.Write((byte) PlayerSessionMessage.Verb);
            message.Write(verb);
            message.Write(uid);
            _networkManager.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
        }

        private void HandleAttachToEntity(NetIncomingMessage message)
        {
            int uid = message.ReadInt32();
            Attach(IoCManager.Resolve<IEntityManagerContainer>().EntityManager.GetEntity(uid));
        }

        #endregion

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
                case PostProcessingEffectType.Acid:
                    e = new AcidPostProcessingEffect(duration);
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

        private void PlayerEntityMoved(object sender, VectorEventArgs args)
        {
            if (OnPlayerMove != null)
                OnPlayerMove(sender, args);
        }

        private void SwitchState(SessionStatus newStatus)
        {
            status = newStatus;
            if (status == SessionStatus.InLobby && RequestedStateSwitch != null)
            {
                RequestedStateSwitch(this, new TypeEventArgs(typeof (Lobby)));
                Detach();
            }
        }
    }
}