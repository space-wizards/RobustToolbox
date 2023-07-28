using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Commands.Vfs;

[ToolshedCommand]
internal sealed class LsCommand : VfsCommand
{
    [CommandImplementation("here")]
    public IEnumerable<ResPath> LsHere([CommandInvocationContext] IInvocationContext ctx)
    {
        var curPath = CurrentPath(ctx);
        return Resources.ContentGetDirectoryEntries(curPath).Select(x => curPath/x);
    }

    [CommandImplementation("in")]
    public IEnumerable<ResPath> LsIn([CommandInvocationContext] IInvocationContext ctx, [CommandArgument] ResPath @in)
    {
        var curPath = CurrentPath(ctx);
        if (@in.IsRooted)
        {
            curPath = @in;
        }
        else
        {
            curPath /= @in;
        }
        return Resources.ContentGetDirectoryEntries(curPath).Select(x => curPath/x);
    }
}
