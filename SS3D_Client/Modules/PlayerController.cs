using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS3D.Atom;
using SS3D.States;
using MOIS;
using Lidgren.Network;
using Mogre;

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
            
            atomManager.mEngine.Camera.DetachFromParent();
            atomManager.mEngine.Camera.Position = new Mogre.Vector3(0, 240, -160) - newAtom.offset;

            SceneNode camNode = controlledAtom.Node.CreateChildSceneNode();
            camNode.AttachObject(atomManager.mEngine.Camera);
            atomManager.mEngine.Camera.SetAutoTracking(true, camNode, new Mogre.Vector3(0, 32, 0));
        }

        public void Detach()
        {
            if (atomManager == null)
                return;
            controlledAtom.attached = false;
            controlledAtom = null;
        }

        public void KeyDown(MOIS.KeyCode k)
        {
            if (atomManager == null)
                return;
            if (controlledAtom == null)
                return;

            controlledAtom.HandleKeyPressed(k);
        }

        public void KeyUp(MOIS.KeyCode k)
        {
            if (atomManager == null)
                return;
            if (controlledAtom == null)
                return;

            controlledAtom.HandleKeyReleased(k);
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
                default:
                    break;
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
            NetOutgoingMessage message = runningState.mEngine.mNetworkMgr.netClient.CreateMessage();
            message.Write((byte)NetMessage.PlayerSessionMessage);
            message.Write((byte)PlayerSessionMessage.Verb);
            message.Write(verb);
            message.Write(uid);
            runningState.mEngine.mNetworkMgr.SendMessage(message, NetDeliveryMethod.ReliableOrdered);
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
