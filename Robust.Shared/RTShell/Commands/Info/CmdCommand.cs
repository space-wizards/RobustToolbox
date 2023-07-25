using System.Collections.Generic;
using System.Linq;
using Robust.Shared.RTShell.Syntax;

namespace Robust.Shared.RTShell.Commands.Info;

[RtShellCommand]
internal sealed class CmdCommand : RtShellCommand
{
    [CommandImplementation("list")]
    public IEnumerable<CommandSpec> List()
        => RtShell.AllCommands();
}
