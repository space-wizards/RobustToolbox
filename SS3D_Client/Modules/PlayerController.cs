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
        GameScreen gameScreen;
        AtomManager atomManager;
        Atom.Atom controlledAtom;

        public PlayerController(GameScreen _gameScreen, AtomManager _atomManager)
        {
            gameScreen = _gameScreen;
            atomManager = _atomManager;
        }

        public void Attach(Atom.Atom newAtom)
        {
            controlledAtom = newAtom;
            controlledAtom.initKeys();
            controlledAtom.attached = true;
            
            atomManager.mEngine.Camera.DetachFromParent();
            atomManager.mEngine.Camera.Position = new Mogre.Vector3(0, 240, -160);

            SceneNode camNode = controlledAtom.Node.CreateChildSceneNode();
            camNode.AttachObject(atomManager.mEngine.Camera);
            atomManager.mEngine.Camera.SetAutoTracking(true, camNode, new Mogre.Vector3(0, 32, 0));
        }

        public void Detach()
        {
            controlledAtom.attached = false;
            controlledAtom = null;
        }

        public void KeyDown(MOIS.KeyCode k)
        {
            if (controlledAtom == null)
                return;

            controlledAtom.HandleKeyPressed(k);
        }

        public void KeyUp(MOIS.KeyCode k)
        {
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

        private void HandleAttachToAtom(NetIncomingMessage message)
        {
            ushort uid = message.ReadUInt16();
            Attach(atomManager.GetAtom(uid));
        }
        #endregion

    }
}
