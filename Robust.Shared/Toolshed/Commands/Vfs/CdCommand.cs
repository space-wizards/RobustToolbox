using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Commands.Vfs;

[ToolshedCommand]
internal sealed class CdCommand : VfsCommand
{
    [CommandImplementation]
    public void Cd(IInvocationContext ctx,ResPath path)
    {
        var curPath = CurrentPath(ctx);

        if (path.IsRooted)
        {
            curPath = path;
        }
        else
        {
            curPath /= path;
        }

        SetPath(ctx, curPath);
    }
}
