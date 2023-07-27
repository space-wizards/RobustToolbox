using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Commands.Vfs;

public abstract class VfsCommand : ToolshedCommand
{
    [Dependency] protected readonly IResourceManager Resources = default!;

    public const string UserVfsLocVariable = "user_vfs_loc";

    public ResPath CurrentPath(IInvocationContext ctx) => ((ResPath?) ctx.ReadVar(UserVfsLocVariable)) ?? ResPath.Root;

    public void SetPath(IInvocationContext ctx, ResPath path)
    {
        ctx.WriteVar(UserVfsLocVariable, (ResPath?) path.Clean());
    }
}
