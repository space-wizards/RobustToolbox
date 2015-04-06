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
        private TimingData _timingData = null;

        public FrameEventArgs(TimingData timingData)
        {
            _timingData = timingData;
        }

        public TimingData TimingData
        {
            get { return _timingData; }
        }

        public float FrameDeltaTime
        {
            get { return (float) (_timingData.FrameDrawTime/1000.0); }
        }
    }
}
