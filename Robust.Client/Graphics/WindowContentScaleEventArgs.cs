using System;

namespace Robust.Client.Graphics
{
    public sealed class WindowContentScaleEventArgs : EventArgs
    {
        public WindowContentScaleEventArgs(IClydeWindow window)
        {
            Window = window;
        }

        public IClydeWindow Window { get; }
    }
}
