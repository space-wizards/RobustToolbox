using System;

namespace Robust.Shared.Analyzers
{
    [Virtual]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class FriendAttribute : Attribute
    {
        public readonly Type[] Friends;

        public FriendAttribute(params Type[] friends)
        {
            Friends = friends;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    public sealed class BestFriendAttribute : FriendAttribute
    {
        public BestFriendAttribute(params Type[] besties) : base(besties)
        {
        }
    }
}
