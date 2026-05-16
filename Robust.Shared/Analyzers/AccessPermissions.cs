using System;
using System.Diagnostics.Contracts;

#if ROBUST_ANALYZERS_IMPL
namespace Robust.Shared.Analyzers.Implementation;
#else
namespace Robust.Shared.Analyzers;
#endif

/// <summary>
///     A set of flags that dictate what kind of field and property access can occur for a given <see cref="AccessAttribute"/>.
/// </summary>
[Flags]
public enum AccessPermissions : byte
{
    None = 0,

    /// <summary>
    ///     Allows field and property read operations, for example using getters, and also <see cref="PureAttribute"/>
    ///     marked methods.
    /// </summary>
    Read    = 1 << 0, // 1
    /// <summary>
    ///     Allows field and property write operations, for example using setters.
    /// </summary>
    Write   = 1 << 1, // 2
    /// <summary>
    ///     Allows executing methods.
    /// </summary>
    Execute = 1 << 2, // 4

    ReadWrite        = Read  | Write,
    ReadExecute      = Read  | Execute,
    WriteExecute     = Write | Execute,
    ReadWriteExecute = Read  | Write | Execute,
}

public static class AccessPermissionsExtensions
{
    public static string ToUnixPermissions(this AccessPermissions permissions)
    {
        return permissions switch
        {
            AccessPermissions.None             => "---",
            AccessPermissions.Read             => "r--",
            AccessPermissions.Write            => "-w-",
            AccessPermissions.Execute          => "--x",
            AccessPermissions.ReadWrite        => "rw-",
            AccessPermissions.ReadExecute      => "r-x",
            AccessPermissions.WriteExecute     => "-wx",
            AccessPermissions.ReadWriteExecute => "rwx",
            _ => throw new ArgumentOutOfRangeException(nameof(permissions), permissions, null)
        };
    }
}
