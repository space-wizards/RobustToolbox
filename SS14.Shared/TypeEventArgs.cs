using System;

namespace SS14.Shared
{
    public class TypeEventArgs : EventArgs
    {
        public Type Type;

        public TypeEventArgs(Type type)
        {
            Type = type;
        }
    }
}