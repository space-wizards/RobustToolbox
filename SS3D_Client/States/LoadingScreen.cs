using System;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Network;

using System.Collections.Generic;
using System.Reflection;

using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
namespace SS3D.States
{
    public class LoadingScreen : State
    {
        private StateManager mStateMgr;
        private NetworkManager mNetworkMgr;

        public LoadingScreen()
        {
        }

        #region Startup, Shutdown, Update
        public override bool Startup(Program _prg)
        {
            prg = _prg;
            mStateMgr = prg.mStateMgr;
            mNetworkMgr = prg.mNetworkMgr;

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
        public override void FormResize()
        {
            throw new NotImplementedException();
        }
        #endregion

    }

}
