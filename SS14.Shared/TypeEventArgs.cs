using System;

namespace SS14.Shared
{
    public class TypeEventArgs : EventArgs
    {
        public Type Type { get; set; }

        public TypeEventArgs(Type type)
        {
            Type = type;
        }

    }
}
