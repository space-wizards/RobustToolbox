using System;
using ClientInterfaces.GOC;
using ClientInterfaces.Network;
using ClientInterfaces.Player;
using ClientInterfaces.State;
using ClientServices.State.States;
using Lidgren.Network;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using SS13_Shared;
using SS13_Shared.GO;
using CGO;

namespace ClientServices.Player
{
    public class PlayerManager : IPlayerManager
    {
        private readonly INetworkManager _networkManager;

        /* Here's the player controller. This will handle attaching GUIS and input to controllable things.
         * Why not just attach the inputs directly? It's messy! This makes the whole thing nicely encapsulated. 
         * This class also communicates with the server to let the server control what entity it is attached to. */
        private IState _currentState;

        public event EventHandler<TypeEventArgs> RequestedStateSwitch;

        public IEntity ControlledEntity { get; private set; }

        public PlayerManager(INetworkManager networkManager)
        {
            _networkManager = networkManager;
        }

        public void Attach(IEntity newEntity)
        {
            ControlledEntity = newEntity;
            ControlledEntity.AddComponent(ComponentFamily.Input, ComponentFactory.Singleton.GetComponent("KeyBindingInputComponent"));
            ControlledEntity.AddComponent(ComponentFamily.Mover, ComponentFactory.Singleton.GetComponent("KeyBindingMoverComponent"));
            ControlledEntity.AddComponent(ComponentFamily.Collider, ComponentFactory.Singleton.GetComponent("ColliderComponent"));
            ControlledEntity.GetComponent(ComponentFamily.Collider).SetParameter(new ComponentParameter("TweakAABB", typeof(Vector4D), new Vector4D(39, 0, 0, 0)));
        }

        public void Detach()
        {
            ControlledEntity.RemoveComponent(ComponentFamily.Input);
            ControlledEntity.RemoveComponent(ComponentFamily.Mover);
            ControlledEntity.RemoveComponent(ComponentFamily.Collider);
            ControlledEntity = null;
        }

        public void SetState(IState state)
        {
            _currentState = state;
        }

        public void KeyDown(KeyboardKeys key)
        {
            if (ControlledEntity == null)
                return;
        }

        public void KeyUp(KeyboardKeys key)
        {
            if (ControlledEntity == null)
                return;
        }

        #region netcode
        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (PlayerSessionMessage)message.ReadByte();
            switch (messageType)
            {
                case PlayerSessionMessage.AttachToEntity:
                    HandleAttachToEntity(message);
                    break;
                case PlayerSessionMessage.JoinLobby:
                    if (RequestedStateSwitch != null)
                    {
                        RequestedStateSwitch(this, new TypeEventArgs(typeof(LobbyScreen)));
                    }
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

    }
}
