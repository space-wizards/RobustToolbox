using System;
using System.Runtime.Serialization;

namespace Robust.Client.GameObjects
{
    [Serializable]
    [Virtual]
    public class ComponentStateApplyException : Exception
    {
        public ComponentStateApplyException()
        {
        }

        public ComponentStateApplyException(string message) : base(message)
        {
        }

        public ComponentStateApplyException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
