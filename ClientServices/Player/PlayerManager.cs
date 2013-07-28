using System;
using System.Collections.Generic;
using System.Linq;
using CGO;
using ClientInterfaces.Network;
using ClientInterfaces.Player;
using ClientServices.Player.PostProcessing;
using ClientServices.State.States;
using GorgonLibrary.Graphics;
using Lidgren.Network;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using SS13_Shared;
using SS13_Shared.GO;
using SS13_Shared.GameStates;
using EntityManager = CGO.EntityManager;
using IEntity = ClientInterfaces.GOC.IEntity;

namespace ClientServices.Player
{
    public class PlayerManager : IPlayerManager
    {
        private readonly INetworkManager _networkManager;

        /* Here's the player controller. This will handle attaching GUIS and input to controllable things.
         * Why not just attach the inputs directly? It's messy! This makes the whole thing nicely encapsulated. 
         * This class also communicates with the server to let the server control what entity it is attached to. */

        private List<PostProcessingEffect> _effects = new List<PostProcessingEffect>(); 

        public event EventHandler<TypeEventArgs> RequestedStateSwitch;
        public event EventHandler<VectorEventArgs> OnPlayerMove;
        private SessionStatus status = SessionStatus.Zombie;

        public IEntity ControlledEntity { get; private set; }

        public PlayerManager(INetworkManager networkManager)
        {
            _networkManager = networkManager;
        }

        public void Update(float frameTime)
        {
            foreach(var e in _effects.ToArray())
            {
                e.Update(frameTime);
            }
        }

        public void Attach(IEntity newEntity)
        {
            ControlledEntity = newEntity;
            ControlledEntity.AddComponent(ComponentFamily.Input, EntityManager.Singleton.ComponentFactory.GetComponent("KeyBindingInputComponent"));
            ControlledEntity.AddComponent(ComponentFamily.Mover, EntityManager.Singleton.ComponentFactory.GetComponent("KeyBindingMoverComponent"));
            ControlledEntity.AddComponent(ComponentFamily.Collider, EntityManager.Singleton.ComponentFactory.GetComponent("ColliderComponent"));
            ControlledEntity.GetComponent(ComponentFamily.Collider).SetParameter(new ComponentParameter("TweakAABB", new Vector4D(39, 0, 0, 0)));
            ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove += PlayerEntityMoved;
        }

        public void AddEffect(PostProcessingEffectType type, float duration)
        {
            PostProcessingEffect e;
            switch(type)
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

        public void ApplyEffects(RenderImage image)
        {
            foreach(var e in _effects)
            {
                e.ProcessImage(image);
            }
        }

        public void Detach()
        {
            ControlledEntity.RemoveComponent(ComponentFamily.Input);
            ControlledEntity.RemoveComponent(ComponentFamily.Mover);
            ControlledEntity.RemoveComponent(ComponentFamily.Collider);
            ControlledEntity.GetComponent<TransformComponent>(ComponentFamily.Transform).OnMove -= PlayerEntityMoved;
            ControlledEntity = null;
        }

        public void KeyDown(KeyboardKeys key)
        {

        }

        public void KeyUp(KeyboardKeys key)
        {

        }

        #region netcode
        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (PlayerSessionMessage)message.ReadByte();
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
                    var effectType = (PostProcessingEffectType)message.ReadInt32();
                    var duration = message.ReadFloat();
                    AddEffect(effectType, duration);
                    break;
            }
        }

        /// <summary>
        /// Verb sender
        /// If UID is 0, it means its a global verb.
        /// </summary>
        /// <param name="verb">the verb</param>
        /// <param name="uid">a target entity's uid</param>
        public void SendVerb(string verb, int uid)
        {
            var message = _networkManager.CreateMessage();
            message.Write((byte)NetMessage.PlayerSessionMessage);
            message.Write((byte)PlayerSessionMessage.Verb);
            message.Write(verb);
            message.Write(uid);
            _networkManager.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
        }

        private void HandleAttachToEntity(NetIncomingMessage message)
        {
            var uid = message.ReadInt32();
            Attach(EntityManager.Singleton.GetEntity(uid));
        }
        #endregion

        private void EffectExpired(PostProcessingEffect effect)
        {
            effect.OnExpired -= EffectExpired;
            if(_effects.Contains(effect))
                _effects.Remove(effect);
        }

        private void PlayerEntityMoved(object sender, VectorEventArgs args)
        {
            if(OnPlayerMove != null)
                OnPlayerMove(sender, args);
        }

        public void ApplyPlayerStates(List<PlayerState> list)
        {
            var myState = list.FirstOrDefault(s => s.UniqueIdentifier == _networkManager.UniqueId);
            if (myState == null)
                return;
            if(myState.ControlledEntity != null && 
                (ControlledEntity == null || (ControlledEntity != null && myState.ControlledEntity != ControlledEntity.Uid) ))
                Attach(EntityManager.Singleton.GetEntity((int)myState.ControlledEntity));
            
            if(status != myState.Status)
                SwitchState(myState.Status);
        }

        private void SwitchState(SessionStatus newStatus)
        {
            status = newStatus;
            if (status == SessionStatus.InLobby && RequestedStateSwitch != null)
            {
                RequestedStateSwitch(this, new TypeEventArgs(typeof (LobbyScreen)));
                Detach();
            }
        }

    }
}
