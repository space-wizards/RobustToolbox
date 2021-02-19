using System;

namespace Robust.Client.Graphics
{
    public class WindowFocusedEventArgs : EventArgs
    {
        public WindowFocusedEventArgs(bool focused)
        {
            Focused = focused;
        }

        public bool Focused { get; }
    }
}
