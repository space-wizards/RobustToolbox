using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS3D.Atom;
using SS3D.States;
using Lidgren.Network;
using GorgonLibrary;
using GorgonLibrary.InputDevices;
using SS3D_shared;
using SS3D_shared.GO;
using CGO;

namespace SS3D.Modules
{
    public class PlayerController
    {
        /* Here's the player controller. This will handle attaching GUIS and input to controllable things.
         * Why not just attach the inputs directly? It's messy! This makes the whole thing nicely encapsulated. 
         * This class also communicates with the server to let the server control what atom it is attached to. */

        public State runningState;
        public AtomManager atomManager;
        public Entity controlledAtom;

        private static PlayerController singleton = null;
        public static PlayerController Singleton
        {
            get
            {
                if (singleton != null)
                    return singleton;
                else
                    throw new TypeInitializationException("PlayerController singleton not initialized.", null);
            }
            set
            {
                singleton = value;
            }
        }

        public static void Initialize(State _runningState, AtomManager _atomManager = null)
        {
            singleton = new PlayerController(_runningState, _atomManager);
        }

        public PlayerController(State _runningState, AtomManager _atomManager)
        {
            runningState = _runningState;
            atomManager = _atomManager;
        }

        public PlayerController(State _runningState)
        {
            runningState = _runningState;
            atomManager = null;
        }

        public void Attach(Entity newAtom)
        {
            if (atomManager == null)
                return;
            controlledAtom = newAtom;
            controlledAtom.AddComponent(ComponentFamily.Input, CGO.ComponentFactory.Singleton.GetComponent("KeyBindingInputComponent"));
            controlledAtom.AddComponent(ComponentFamily.Mover, CGO.ComponentFactory.Singleton.GetComponent("KeyBindingMoverComponent"));
            controlledAtom.AddComponent(ComponentFamily.Collider, CGO.ComponentFactory.Singleton.GetComponent("ColliderComponent"));
            controlledAtom.GetComponent(ComponentFamily.Collider).SetParameter(new CGO.ComponentParameter("TweakAABB", typeof(Vector4D), new Vector4D(39, 0, 0, 0)));
        }

        public void Detach()
        {
            if (atomManager == null)
                return;
            controlledAtom = null;
            controlledAtom.RemoveComponent(ComponentFamily.Input);
            controlledAtom.RemoveComponent(ComponentFamily.Mover);
            controlledAtom.RemoveComponent(ComponentFamily.Collider);
        }

        public void KeyDown(KeyboardKeys key)
        {
            if (atomManager == null)
                return;
            if (controlledAtom == null)
                return;

            //controlledAtom.HandleKeyPressed(key);
        }

        public void KeyUp(KeyboardKeys key)
        {
            if (atomManager == null)
                return;
            if (controlledAtom == null)
                return;

            //controlledAtom.HandleKeyReleased(key);
        }

        #region netcode
        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            PlayerSessionMessage messageType = (PlayerSessionMessage)message.ReadByte();
            switch (messageType)
            {
                case PlayerSessionMessage.AttachToAtom:
                    HandleAttachToAtom(message);
                    break;
                case PlayerSessionMessage.JoinLobby:
                    runningState.prg.mStateMgr.RequestStateChange(typeof(LobbyScreen));
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
        //            if (runningState.GetType() == System.Type.GetType("SS3D.States.GameScreen"))
        //            {
        //                GameScreen g = (GameScreen)runningState;
        //                g.guiComponents[GuiComponentType.StatPanelComponent].HandleNetworkMessage(message);
        //            }
        //            break;
        //        case GuiComponentType.AppendagesComponent:
        //            if (runningState.GetType() == System.Type.GetType("SS3D.States.GameScreen"))
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
        /// <param name="uid">a target atom's uid</param>
        public void SendVerb(string verb, int uid)
        {
            NetOutgoingMessage message = runningState.prg.mNetworkMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.PlayerSessionMessage);
            message.Write((byte)PlayerSessionMessage.Verb);
            message.Write(verb);
            message.Write(uid);
            runningState.prg.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
        }

        private void HandleAttachToAtom(NetIncomingMessage message)
        {
            if (atomManager == null)
                return;
            int uid = message.ReadInt32();
            Attach(atomManager.GetAtom(uid));
        }
        #endregion

    }
}
