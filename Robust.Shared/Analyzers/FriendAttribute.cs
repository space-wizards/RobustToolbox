using System;

namespace Robust.Shared.Analyzers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class FriendAttribute : Attribute
    {
        public readonly Type[] Friends;

        public FriendAttribute(params Type[] friends)
        {
            Friends = friends;
        }
    }
}
