using System;
using JetBrains.Annotations;

namespace Robust.Shared.Reflection
{
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ReflectRaiseEventAttribute : Attribute
    {
        public readonly Type EventType;
        public readonly bool Broadcast;

        public ReflectRaiseEventAttribute(Type eventType, bool broadcast = false)
        {
            EventType = eventType;
            Broadcast = broadcast;
        }
    }
}
