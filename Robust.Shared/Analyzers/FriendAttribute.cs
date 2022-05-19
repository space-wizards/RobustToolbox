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
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct
                    | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor)]
    public sealed class FriendAttribute : Attribute
    {
        public readonly Type[] Friends;

        /// <summary>
        ///     Access permissions for the type itself, or the type containing the member.
        /// </summary>
        public AccessPermissions Self { get; init; }   = AccessPermissions.ReadWriteExecute;
        public AccessPermissions Friend { get; init; } = AccessPermissions.ReadWriteExecute;
        public AccessPermissions Other { get; init; }  = AccessPermissions.Read;

        public FriendAttribute(params Type[] friends)
        {
            Friends = friends;
        }
    }

    [Flags]
    public enum AccessPermissions : byte
    {
        None = 0,

        Read    = 1 << 0, // 1
        Write   = 1 << 1, // 2
        Execute = 1 << 2, // 4

        ReadWrite        = Read  | Write,
        ReadExecute      = Read  | Execute,
        WriteExecute     = Write | Execute,
        ReadWriteExecute = Read  | Write | Execute,
    }
}
