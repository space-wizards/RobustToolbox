using System;
using Robust.Shared.GameObjects;

namespace Robust.Shared.GameStates
{
    /// <summary>
    /// This attribute marks a component as networked, so that it is replicated to clients.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class NetworkedComponentAttribute : Attribute
    {
        /// <summary>
        ///     Optionally defines which type to use for automatic component state creation, if <see cref="ComponentGetState"/>
        ///     is not handled. Must be an inheritor of <see cref="ComponentState"/>.
        /// </summary>
        public Type? ComponentStateType;

        public NetworkedComponentAttribute(Type? componentStateType=null)
        {
            ComponentStateType = componentStateType;
        }
    }
}
