using System;

namespace Robust.Shared.Analyzers
{
    /// <summary>
    ///     If this attribute is present on a type, only the types specified in the attribute will be able to
    ///     write/execute members in the type. Any type is free to read any member, however.
    /// </summary>
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

    /// <summary>
    ///     If this attribute is present on a type, only the types specified in the attribute will be able to
    ///     read/write/execute members in the type.
    /// </summary>
    /// <remarks>This attribute is the same as <see cref="FriendAttribute"/> but even more restrictive.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct)]
    public sealed class BestFriendAttribute : FriendAttribute
    {
        public BestFriendAttribute(params Type[] besties) : base(besties)
        {
        }
    }
}
