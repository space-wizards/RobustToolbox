using System;
using SS14.Shared.Maths;

namespace SS14.Shared
{
    public class VectorEventArgs : EventArgs
    {
        public VectorEventArgs(Vector2 vectorFrom, Vector2 vectorTo)
        {
            VectorFrom = vectorFrom;
            VectorTo = vectorTo;
        }

        public Vector2 VectorFrom { get; }
        public Vector2 VectorTo { get; }
    }
}
