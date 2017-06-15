using System;

namespace SS14.Client.Graphics.Event
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
