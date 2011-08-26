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

namespace SS3D.Modules
{
    public class PlayerController
    {
        /* Here's the player controller. This will handle attaching GUIS and input to controllable things.
         * Why not just attach the inputs directly? It's messy! This makes the whole thing nicely encapsulated. 
         * This class also communicates with the server to let the server control what atom it is attached to. */
        public State runningState;
        public AtomManager atomManager;
        public Atom.Atom controlledAtom;

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

        public void Attach(Atom.Atom newAtom)
        {
            if (atomManager == null)
                return;
            controlledAtom = newAtom;
            controlledAtom.initKeys();
            controlledAtom.attached = true;
            
            /*atomManager.mEngine.Camera.DetachFromParent();
            atomManager.mEngine.Camera.Position = new Mogre.Vector3(-80, 240, -80) - newAtom.offset;
            float scale = 3f; // Your scale here.

            
            Matrix4 p = BuildScaledOrthoMatrix(atomManager.mEngine.Window.Width / scale / -2.0f,
                                                    atomManager.mEngine.Window.Width / scale / 2.0f,
                                                    atomManager.mEngine.Window.Height / scale / -2.0f,
                                                    atomManager.mEngine.Window.Height / scale / 2.0f, 0, 1000);

            atomManager.mEngine.Camera.SetCustomProjectionMatrix(true, p);
            atomManager.mEngine.Camera.ProjectionType = ProjectionType.PT_ORTHOGRAPHIC;

            SceneNode camNode = controlledAtom.Node.CreateChildSceneNode();
            camNode.AttachObject(atomManager.mEngine.Camera);
            atomManager.mEngine.Camera.SetAutoTracking(true, camNode, new Mogre.Vector3(0, 32, 0));*/
        }

        /*public Matrix4 BuildScaledOrthoMatrix(float left, float right, float bottom, float top, float near, float far)
        {
            float invw = 1 / (right - left);
            float invh = 1 / (top - bottom);
            float invd = 1 / (far - near);

            Matrix4 proj = Matrix4.ZERO;
            proj[0, 0] = 2 * invw;
            proj[0, 3] = -(right + left) * invw;
            proj[1, 1] = 2 * invh;
            proj[1, 3] = -(top + bottom) * invh;
            proj[2, 2] = -2 * invd;
            proj[2, 3] = -(far + near) * invd;
            proj[3, 3] = 1;

            return proj;
        }*/

        public void Detach()
        {
            if (atomManager == null)
                return;
            controlledAtom.attached = false;
            controlledAtom = null;
        }

        public void KeyDown(KeyboardKeys key)
        {
            if (atomManager == null)
                return;
            if (controlledAtom == null)
                return;

            controlledAtom.HandleKeyPressed(key);
        }

        public void KeyUp(KeyboardKeys key)
        {
            if (atomManager == null)
                return;
            if (controlledAtom == null)
                return;

            controlledAtom.HandleKeyReleased(key);
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
                case PlayerSessionMessage.UIComponentMessage:
                    HandleUIComponentMessage(message);
                    break;
                default:
                    break;
            }
        }

        private void HandleUIComponentMessage(NetIncomingMessage message)
        {
            GuiComponentType component = (GuiComponentType)message.ReadByte();
            switch (component)
            {
                case GuiComponentType.HealthComponent:
                    if (runningState.GetType() == System.Type.GetType("SS3D.States.GameScreen"))
                    {
                        GameScreen g = (GameScreen)runningState;
                        g.guiComponents[GuiComponentType.HealthComponent].HandleNetworkMessage(message);
                    }
                    break;
                case GuiComponentType.AppendagesComponent:
                    if (runningState.GetType() == System.Type.GetType("SS3D.States.GameScreen"))
                    {
                        GameScreen g = (GameScreen)runningState;
                        g.guiComponents[GuiComponentType.AppendagesComponent].HandleNetworkMessage(message);
                    }
                    break;
                default:break;
            }
        }

        /// <summary>
        /// Verb sender
        /// If UID is 0, it means its a global verb.
        /// </summary>
        /// <param name="verb">the verb</param>
        /// <param name="uid">a target atom's uid</param>
        public void SendVerb(string verb, ushort uid)
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
            ushort uid = message.ReadUInt16();
            Attach(atomManager.GetAtom(uid));
        }
        #endregion

    }
}
