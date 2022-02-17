using System;

namespace Robust.Shared.Reflection
{
    public sealed class ReflectionUpdateEventArgs : EventArgs
    {
        public readonly IReflectionManager ReflectionManager;
        public ReflectionUpdateEventArgs(IReflectionManager reflectionManager)
        {
            ReflectionManager = reflectionManager;
        }
    }
}
