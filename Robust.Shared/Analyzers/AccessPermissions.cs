using System;

#if ROBUST_ANALYZERS_IMPL
namespace Robust.Shared.Analyzers.Implementation;
#else
namespace Robust.Shared.Analyzers;
#endif

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
