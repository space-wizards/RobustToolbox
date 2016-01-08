using System;
using SS14.Shared.Maths;
using SFML.System;

namespace SS14.Shared
{
    public class VectorEventArgs : EventArgs
    {
        public VectorEventArgs(Vector2f vectorFrom, Vector2f vectorTo)
        {
            VectorFrom = vectorFrom;
            VectorTo = vectorTo;
        }

        public Vector2f VectorFrom { get; private set; }
        public Vector2f VectorTo { get; private set; }
    }
}