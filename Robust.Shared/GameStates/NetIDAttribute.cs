using System;
using Robust.Shared.GameObjects;

namespace Robust.Shared.GameStates
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class NetIDAttribute : Attribute
    {
        /// <summary>
        ///     Represents the network ID for the component.
        ///     The network ID is used to determine which component will receive the component state
        ///     on the other side of the network.
        ///     If this is <c>null</c>, the component is not replicated across the network.
        /// </summary>
        /// <seealso cref="IComponentRegistration.NetID" />
        public uint NetId { get; }

        public NetIDAttribute(uint netId)
        {
            NetId = netId;
        }
    }
}
