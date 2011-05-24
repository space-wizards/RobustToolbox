using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS3D.Atom;
using SS3D.States;
using MOIS;

namespace SS3D.Modules
{
    public class PlayerController
    {
        /* Here's the player controller. This will handle attaching GUIS and input to controllable things.
         * Why not just attach the inputs directly? It's messy! This makes the whole thing nicely encapsulated. */
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
        }

        public void Detach()
        {
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
    }
}
