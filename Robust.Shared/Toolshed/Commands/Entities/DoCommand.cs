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
    public IEnumerable<T> Do<T>(
        IInvocationContext ctx,
        [PipedArgument] IEnumerable<T> input,
        string command)
    {
        if (ctx is not OldShellInvocationContext { } reqCtx || reqCtx.Shell == null)
        {
            throw new NotImplementedException("do can only be executed in a shell invocation context. Some commands like emplace provide their own context.");
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
                var localPos = xformQ.GetComponent(id).Coordinates;
                cmdStr = cmdStr
                    .Replace("$ID", id.ToString())
                    .Replace("$PID", (reqCtx.Session?.AttachedEntity ?? EntityUid.Invalid).ToString())
                    .Replace("$WX", worldPos.X.ToString(CultureInfo.InvariantCulture))
                    .Replace("$WY", worldPos.Y.ToString(CultureInfo.InvariantCulture))
                    .Replace("$LX", localPos.X.ToString(CultureInfo.InvariantCulture))
                    .Replace("$LY", localPos.Y.ToString(CultureInfo.InvariantCulture));
            }

            cmdStr = cmdStr.Replace("$SELF", i!.ToString() ?? "");
            shell.ExecuteCommand(cmdStr);

            yield return i;
        }
    }
}
