using System;
using System.Collections.Generic;

namespace SS3D.Modules
{
  /************************************************************************/
  /* base class for program states                                        */
  /************************************************************************/
  public abstract class State
  {
    public State() //constructor
    {
       
    }

    public abstract bool Startup( StateManager _mgr );

    public abstract void Shutdown();

    public abstract void Update( long _frameTime );

    public abstract void UpdateInput(Mogre.FrameEvent evt, MOIS.Keyboard keyState, MOIS.Mouse mouseState);
    public abstract void KeyDown(MOIS.KeyEvent keyState);
    public abstract void KeyUp(MOIS.KeyEvent keyState);
    public abstract void MouseUp(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button);
    public abstract void MouseDown(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button);
    public abstract void MouseMove(MOIS.MouseEvent mouseState);

  }

}
