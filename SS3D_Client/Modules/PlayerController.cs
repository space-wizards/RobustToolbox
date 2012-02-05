using System;
using SS13.States;
using Lidgren.Network;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using SS13_Shared.GO;
using CGO;

namespace SS13.Modules
{
    public class PlayerController
    {
        /* Here's the player controller. This will handle attaching GUIS and input to controllable things.
         * Why not just attach the inputs directly? It's messy! This makes the whole thing nicely encapsulated. 
         * This class also communicates with the server to let the server control what entity it is attached to. */

        public State RunningState { get; private set; }
        public Entity ControlledEntity { get; private set; }

        private static PlayerController _singleton;
        public static PlayerController Singleton
        {
            get
            {
                if (_singleton != null)
                    return _singleton;
                else
                    throw new TypeInitializationException("PlayerController singleton not initialized.", null);
            }
            set
            {
                _singleton = value;
            }
        }

        public static void Initialize(State _runningState)
        {
            _singleton = new PlayerController(_runningState);
        }

        public PlayerController(State _runningState)
        {
            RunningState = _runningState;
        }

        public void Attach(Entity newEntity)
        {
            ControlledEntity = newEntity;
            ControlledEntity.AddComponent(ComponentFamily.Input, CGO.ComponentFactory.Singleton.GetComponent("KeyBindingInputComponent"));
            ControlledEntity.AddComponent(ComponentFamily.Mover, CGO.ComponentFactory.Singleton.GetComponent("KeyBindingMoverComponent"));
            ControlledEntity.AddComponent(ComponentFamily.Collider, CGO.ComponentFactory.Singleton.GetComponent("ColliderComponent"));
            ControlledEntity.GetComponent(ComponentFamily.Collider).SetParameter(new CGO.ComponentParameter("TweakAABB", typeof(Vector4D), new Vector4D(39, 0, 0, 0)));
        }

        public void Detach()
        {
            ControlledEntity = null;
            ControlledEntity.RemoveComponent(ComponentFamily.Input);
            ControlledEntity.RemoveComponent(ComponentFamily.Mover);
            ControlledEntity.RemoveComponent(ComponentFamily.Collider);
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
            PlayerSessionMessage messageType = (PlayerSessionMessage)message.ReadByte();
            switch (messageType)
            {
                case PlayerSessionMessage.AttachToEntity:
                    HandleAttachToEntity(message);
                    break;
                case PlayerSessionMessage.JoinLobby:
                    RunningState.Program.StateManager.RequestStateChange(typeof(LobbyScreen));
                    break;
                default:
                    break;
            }
        }

        //private void HandleUIComponentMessage(NetIncomingMessage message)
        //{
        //    GuiComponentType component = (GuiComponentType)message.ReadByte();
        //    switch (component)
        //    {
        //        case GuiComponentType.HealthComponent:
        //            if (runningState.GetType() == System.Type.GetType("SS13.States.GameScreen"))
        //            {
        //                GameScreen g = (GameScreen)runningState;
        //                g.guiComponents[GuiComponentType.StatPanelComponent].HandleNetworkMessage(message);
        //            }
        //            break;
        //        case GuiComponentType.AppendagesComponent:
        //            if (runningState.GetType() == System.Type.GetType("SS13.States.GameScreen"))
        //            {
        //                GameScreen g = (GameScreen)runningState;
        //                g.guiComponents[GuiComponentType.AppendagesComponent].HandleNetworkMessage(message);
        //            }
        //            break;
        //        default:break;
        //    }
        //}

        /// <summary>
        /// Verb sender
        /// If UID is 0, it means its a global verb.
        /// </summary>
        /// <param name="verb">the verb</param>
        /// <param name="uid">a target entity's uid</param>
        public void SendVerb(string verb, int uid)
        {
            NetOutgoingMessage message = RunningState.Program.NetworkManager.netClient.CreateMessage();
            message.Write((byte)NetMessage.PlayerSessionMessage);
            message.Write((byte)PlayerSessionMessage.Verb);
            message.Write(verb);
            message.Write(uid);
            RunningState.Program.NetworkManager.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
        }

        private void HandleAttachToEntity(NetIncomingMessage message)
        {
            int uid = message.ReadInt32();
            Attach(EntityManager.Singleton.GetEntity(uid));
        }
        #endregion

    }
}
