using System.Collections.Generic;
using System.Linq;
using Robust.Shared.RTShell.Syntax;

namespace Robust.Shared.RTShell.Commands.Info;

[ConsoleCommand]
internal sealed class CmdCommand : ConsoleCommand
{
    [CommandImplementation("list")]
    public IEnumerable<CommandSpec> List()
        => RtShell.AllCommands();
}
