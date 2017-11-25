using System;

namespace SS14.Shared
{
    public class TypeEventArgs : EventArgs
    {
        private Type type;

        public TypeEventArgs(Type type)
        {
            Type = type;
        }

        public Type Type { get => type; set => type = value; }
    }
}
