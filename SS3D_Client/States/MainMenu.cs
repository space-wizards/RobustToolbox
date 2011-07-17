using System.Collections.Generic;
using System.IO;
using System.Linq;
using Miyagi;
using Miyagi.UI;
using Miyagi.UI.Controls;
using Miyagi.Common;
using Miyagi.Common.Data;
using Miyagi.Common.Resources;
using Miyagi.Common.Events;
using Miyagi.TwoD;
using Mogre;

using GorgonLibrary;
using GorgonLibrary.InputDevices;
using SS3D.Modules;

namespace SS3D.States
{
  public class MainMenu : State
  {
    private StateManager mStateMgr;
    private GUI guiMainMenu;
    private GUI guiBackground;
    private Label infoLabel;

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

    public override void Update(long _frameTime)
    {
    } 
    #endregion
    public override void GorgonRender()
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
