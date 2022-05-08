using System;

namespace Robust.Shared.Analyzers
{
    /// <summary>
    ///     If this attribute is present on a type, only the types specified in the attribute will be able to
    ///     write/execute members in the type. <br/>
    ///     <br/>
    ///     If this attribute is present on a type's member, only the types specified in the attribute will be able to
    ///     write/execute that member. <br/>
    /// </summary>
    /// <remarks>This attribute does not restrict read access. See <see cref="BestFriendAttribute"/> for that.</remarks>
    [Virtual]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct
                    | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor)]
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
    ///     read/write/execute members in the type.<br/>
    ///     <br/>
    ///     If this attribute is present on a type's member, only the types specified in the attribute will be able to
    ///     read/write/execute that member.
    /// </summary>
    /// <remarks>This attribute is the same as <see cref="FriendAttribute"/> but even more restrictive.</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct
                    | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method  | AttributeTargets.Constructor)]
    public sealed class BestFriendAttribute : FriendAttribute
    {
        public BestFriendAttribute(params Type[] besties) : base(besties)
        {
        }
    }
}
