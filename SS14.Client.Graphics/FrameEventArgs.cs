using System;

namespace SS14.Client.Graphics
{
    public class FrameEventArgs : EventArgs
    {
        public float Elapsed { get; }

        public FrameEventArgs(float elapsed)
        {
            Elapsed = elapsed;
        }
    }
}
