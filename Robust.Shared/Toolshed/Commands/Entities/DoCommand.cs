using System;
using System.Collections.Generic;
using System.Globalization;
using Robust.Shared.GameObjects;
using Robust.Shared.Toolshed.Invocation;

namespace Robust.Shared.Toolshed.Commands.Entities;

[ToolshedCommand]
internal sealed class DoCommand : ToolshedCommand
{
    private SharedTransformSystem? _xformSys;

    [CommandImplementation, TakesPipedTypeAsGeneric]
    public void Do<T>(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> input,
        [CommandArgument] string command)
    {
        if (ctx is not OldShellInvocationContext { } reqCtx)
        {
            throw new NotImplementedException();
        }

        _xformSys ??= GetSys<SharedTransformSystem>();
        var xformQ = GetEntityQuery<TransformComponent>();
        var shell = reqCtx.Shell;
        foreach (var i in input)
        {
            var cmdStr = command;
            if (i is EntityUid id)
            {
                var worldPos = _xformSys.GetWorldPosition(id, xformQ);
                cmdStr = cmdStr
                    .Replace("$ID", id.ToString())
                    .Replace("$WX", worldPos.X.ToString(CultureInfo.InvariantCulture))
                    .Replace("$WY", worldPos.Y.ToString(CultureInfo.InvariantCulture));
            }

            cmdStr = cmdStr.Replace("$SELF", i!.ToString() ?? "");
            shell.ExecuteCommand(cmdStr);
        }
    }
}
