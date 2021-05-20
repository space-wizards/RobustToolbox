using System;

namespace Robust.Client.Graphics
{
    public class WindowFocusedEventArgs : EventArgs
    {
        public WindowFocusedEventArgs(bool focused, IClydeWindow window)
        {
            Focused = focused;
            Window = window;
        }

        public bool Focused { get; }
        public IClydeWindow Window { get; }
    }
}
