using System;

namespace Robust.Shared.GameStates
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class NetIDAttribute : Attribute
    {
        public uint NetId { get; }

        public NetIDAttribute(uint netId)
        {
            NetId = netId;
        }
    }
}
