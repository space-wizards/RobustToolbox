using System;

namespace SS13_Shared
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