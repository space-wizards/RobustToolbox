using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SS3D.Atom;
using SS3D.States;

namespace SS3D.Modules
{
    public class PlayerController
    {
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

        }


    }
}
