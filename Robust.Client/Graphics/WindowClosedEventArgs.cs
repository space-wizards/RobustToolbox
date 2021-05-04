using System;

namespace Robust.Client.Graphics
{
    public class WindowClosedEventArgs : EventArgs
    {
        public IClydeWindow Window { get; }

        public WindowClosedEventArgs(IClydeWindow window)
        {
            Window = window;
        }
    }
}
