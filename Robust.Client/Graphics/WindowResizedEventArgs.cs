using System;
using Robust.Shared.Maths;

namespace Robust.Client.Graphics
{
    public class WindowResizedEventArgs : EventArgs
    {
        public WindowResizedEventArgs(Vector2i oldSize, Vector2i newSize, IClydeWindow window)
        {
            OldSize = oldSize;
            NewSize = newSize;
            Window = window;
        }

        public Vector2i OldSize { get; }
        public Vector2i NewSize { get; }
        public IClydeWindow Window { get; }
    }
}
