using System.Collections.Generic;
using System.IO;
using System.Linq;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS3D.Modules;

namespace SS3D.States
{
  public class MainMenu : State
  {
    private StateManager mStateMgr;

    public MainMenu()
    {
    }

    #region Startup, Shutdown, Update
    public override bool Startup(Program _prg)
    {
        prg = _prg;
        mStateMgr = prg.mStateMgr;

        return true;
    }

      public override void Shutdown()
    {
    }

      public override void Update(FrameEventArgs e)
    {
    } 
    #endregion
    public override void GorgonRender(FrameEventArgs e)
    {

        return;
    }
    public override void FormResize()
    {
    }
    #region Input

   
    public override void KeyDown(KeyboardInputEventArgs e)
    { }
    public override void KeyUp(KeyboardInputEventArgs e)
    { }
    public override void MouseUp(MouseInputEventArgs e)
    { }
    public override void MouseDown(MouseInputEventArgs e)
    { }
    public override void MouseMove(MouseInputEventArgs e)
    { }

    #endregion

  }

}
