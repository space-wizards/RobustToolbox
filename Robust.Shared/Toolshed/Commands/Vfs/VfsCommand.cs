using System;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Commands.Vfs;

/// <summary>
///     A simple base class for commands that work with the VFS and would like to manipulate the user's current location within the VFS.
/// </summary>
/// <seealso cref="UserVfsLocVariableName"/>
[PublicAPI]
public abstract class VfsCommand : ToolshedCommand
{
    [Dependency] protected readonly IResourceManager Resources = default!;

    /// <summary>
    ///     The name of the variable storing a <see cref="ResPath">ResPath?</see> representing the user's current VFS location.
    /// </summary>
    public const string UserVfsLocVariableName = "user_vfs_loc";

    protected ResPath CurrentPath(IInvocationContext ctx) => ((ResPath?) ctx.ReadVar(UserVfsLocVariableName)) ?? ResPath.Root;

    protected void SetPath(IInvocationContext ctx, ResPath path)
    {
        ctx.WriteVar(UserVfsLocVariableName, (ResPath?) path.Clean());
    }
}
