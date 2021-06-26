using System;

namespace Robust.Shared.GameStates
{
    /// <summary>
    /// This attribute marks a component as networked, so that it is replicated to clients.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class NetIDAttribute : Attribute { }
}
