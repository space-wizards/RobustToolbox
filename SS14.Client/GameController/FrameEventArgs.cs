using System;

namespace SS14.Client
{
    // This is probably a terrible folder to put this but I can't think of anything else.
    public class FrameEventArgs : EventArgs
    {
        public float Elapsed { get; }

        public FrameEventArgs(float elapsed)
        {
            Elapsed = elapsed;
        }
    }
}
