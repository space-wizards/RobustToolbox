using System;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Defines Name that this component is represented with in prototypes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ComponentProtoNameAttribute : Attribute
    {
        public string PrototypeName { get; }

        public ComponentProtoNameAttribute(string prototypeName)
        {
            PrototypeName = prototypeName;
        }
    }
}
