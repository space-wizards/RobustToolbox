using System;
using JetBrains.Annotations;

namespace Robust.Shared.Network
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class NetIDAttribute : Attribute
    {
        public NetIDAttribute([UsedImplicitly]string netId) {}
    }
}
