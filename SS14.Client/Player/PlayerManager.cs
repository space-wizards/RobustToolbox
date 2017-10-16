using Lidgren.Network;
using SFML.Window;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.GameObjects;
using SS14.Client.Graphics.Render;
using SS14.Client.Interfaces.Network;
using SS14.Client.Interfaces.Player;
using SS14.Client.Player.PostProcessing;
using SS14.Client.State.States;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Interfaces.Network;

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

        #region IPlayerManager Members

        public event EventHandler<TypeEventArgs> RequestedStateSwitch;
        public event EventHandler<MoveEventArgs> OnPlayerMove;

        public IEntity ControlledEntity { get; private set; }

        public void Update(float frameTime)
        {
            foreach (PostProcessingEffect e in _effects.ToArray())
            {
                e.Update(frameTime);
            }
        }

        public void Attach(IEntity newEntity)
        {
            // Detach and cleanup first
            Detach();

            var factory = IoCManager.Resolve<IComponentFactory>();

            ControlledEntity = newEntity;
            ControlledEntity.AddComponent(factory.GetComponent<KeyBindingInputComponent>());
            if (ControlledEntity.HasComponent<IMoverComponent>())
            {
                ControlledEntity.RemoveComponent<IMoverComponent>();
            }
            ControlledEntity.AddComponent(factory.GetComponent<PlayerInputMoverComponent>());
            if (!ControlledEntity.HasComponent<CollidableComponent>())
            {
                ControlledEntity.AddComponent(factory.GetComponent<CollidableComponent>());
            }

            ControlledEntity.GetComponent<ITransformComponent>().OnMove += PlayerEntityMoved;
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
                ControlledEntity.RemoveComponent<KeyBindingInputComponent>();
                ControlledEntity.RemoveComponent<PlayerInputMoverComponent>();
                ControlledEntity.RemoveComponent<CollidableComponent>();
                var transform = ControlledEntity.GetComponent<ITransformComponent>();
                if (transform != null)
                {
                    transform.OnMove -= PlayerEntityMoved;
                }
            }
            ControlledEntity = null;
        }

        public void KeyDown(Keyboard.Key key)
        {
            //TODO: Figure out what to do with this
        }

        public void KeyUp(Keyboard.Key key)
        {
            //TODO: Figure out what to do with this
        }

        public void ApplyPlayerStates(List<PlayerState> list)
        {
            PlayerState myState = list.FirstOrDefault(s => s.UniqueIdentifier == _networkManager.Peer.UniqueIdentifier);
            if (myState == null)
                return;
            if (myState.ControlledEntity != null &&
                (ControlledEntity == null ||
                 (ControlledEntity != null && myState.ControlledEntity != ControlledEntity.Uid)))
                Attach(
                    IoCManager.Resolve<IEntityManager>().GetEntity((int)myState.ControlledEntity));

            if (status != myState.Status)
                SwitchState(myState.Status);
        }

        #endregion IPlayerManager Members

        #region netcode

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (PlayerSessionMessage)message.ReadByte();
            switch (messageType)
            {
                case PlayerSessionMessage.AttachToEntity:
                    break;
                case PlayerSessionMessage.JoinLobby:
                    break;
                case PlayerSessionMessage.AddPostProcessingEffect:
                    var effectType = (PostProcessingEffectType)message.ReadInt32();
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
            message.Write((byte)NetMessages.PlayerSessionMessage);
            message.Write((byte)PlayerSessionMessage.Verb);
            message.Write(verb);
            message.Write(uid);
            _networkManager.ClientSendMessage(message, NetDeliveryMethod.ReliableOrdered);
        }

        private void HandleAttachToEntity(NetIncomingMessage message)
        {
            int uid = message.ReadInt32();
            Attach(IoCManager.Resolve<IEntityManager>().GetEntity(uid));
        }

        #endregion netcode

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

        private void PlayerEntityMoved(object sender, MoveEventArgs args)
        {
            if (OnPlayerMove != null)
                OnPlayerMove(sender, args);
        }

        private void SwitchState(SessionStatus newStatus)
        {
            status = newStatus;
            if (status == SessionStatus.InLobby && RequestedStateSwitch != null)
            {
                RequestedStateSwitch(this, new TypeEventArgs(typeof(Lobby)));
                Detach();
            }
        }
    }
}
