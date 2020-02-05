using System;
using Robust.Shared.Maths;

namespace Robust.Client.Interfaces.Graphics
{
    public class WindowResizedEventArgs : EventArgs
    {
        public WindowResizedEventArgs(Vector2i oldSize, Vector2i newSize)
        {
            OldSize = oldSize;
            NewSize = newSize;
        }

        public Vector2i OldSize { get; }
        public Vector2i NewSize { get; }
    }
}