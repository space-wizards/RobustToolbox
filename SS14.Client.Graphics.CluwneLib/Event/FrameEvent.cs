using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SS14.Client.Graphics.CluwneLib.Timing;

namespace SS14.Client.Graphics.CluwneLib.Event
{
    public delegate void FrameEventHandler(object sender, FrameEventArgs e);


    public class FrameEventArgs : EventArgs
    {
        private float _frameDeltaTime;

        public FrameEventArgs(float frameDeltaTime)
        {
            _frameDeltaTime = frameDeltaTime;
        }

        public float FrameDeltaTime
        {
            get { return _frameDeltaTime; }
        }
    }
}
