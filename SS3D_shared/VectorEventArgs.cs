using System;
using GorgonLibrary;

namespace SS13_Shared
{
    public class VectorEventArgs : EventArgs
    {
        public Vector2 VectorFrom { get; private set; }
        public Vector2 VectorTo { get; private set; }
        
        public VectorEventArgs(Vector2 vectorFrom, Vector2 vectorTo)
        {
            VectorFrom = vectorFrom;
            VectorTo = vectorTo;
        }
    }
}
