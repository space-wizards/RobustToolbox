using System;
using JetBrains.Annotations;

namespace Robust.Shared.Network
{
    [AttributeUsage(AttributeTargets.Class)]
    [BaseTypeRequired(typeof(NetMessage))]
    public class NetMessageAttribute : Attribute
    {
        public NetMessageAttribute(MsgGroups group, string? name = null)
        {
            Group = group;
            Name = name;
        }

        public string? Name { get; }

        public MsgGroups Group { get; }
    }
}
