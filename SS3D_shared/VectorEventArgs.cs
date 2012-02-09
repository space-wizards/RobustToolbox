using System;
using GorgonLibrary;

namespace SS13_Shared
{
    public class VectorEventArgs : EventArgs
    {
        public Vector2D Vector2D { get; private set; }

        public VectorEventArgs(Vector2D vector)
        {
            Vector2D = vector;
        }
    }
}
