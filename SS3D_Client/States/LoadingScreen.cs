using System;

using Mogre;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Network;

using System.Collections.Generic;
using System.Reflection;

namespace SS3D.States
{
    public class LoadingScreen : State
    {
        private StateManager mStateMgr;
        private NetworkManager mNetworkMgr;

        public LoadingScreen()
        {
            mEngine = null;
        }

        #region Startup, Shutdown, Update
        public override bool Startup(StateManager _mgr)
        {
            mEngine = _mgr.Engine;
            mStateMgr = _mgr;
            mNetworkMgr = mEngine.mNetworkMgr;

            return true;
        }

        public override void Shutdown()
        {
        }

        public override void Update(long _frameTime)
        {
        }
        #endregion

        #region Input
        public override void UpdateInput(Mogre.FrameEvent evt, MOIS.Keyboard keyState, MOIS.Mouse mouseState)
        {
        }

        public override void KeyDown(MOIS.KeyEvent keyState)
        {
        }

        public override void KeyUp(MOIS.KeyEvent keyState)
        {
        }

        public override void MouseUp(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
        {
        }

        public override void MouseDown(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
        {
        }

        public override void MouseMove(MOIS.MouseEvent mouseState)
        {
        }

        #endregion

    }

}
