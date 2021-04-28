using System;

namespace Robust.Client.Graphics
{
    public class WindowDestroyedEventArgs : EventArgs
    {
        public IClydeWindow Window { get; }

        public WindowDestroyedEventArgs(IClydeWindow window)
        {
            Window = window;
        }
    }
}
