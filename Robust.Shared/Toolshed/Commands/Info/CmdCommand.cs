using System.Collections.Generic;

namespace Robust.Shared.Toolshed.Commands.Info;

[RtShellCommand]
internal sealed class CmdCommand : ToolshedCommand
{
    [CommandImplementation("list")]
    public IEnumerable<CommandSpec> List()
        => Toolshed.AllCommands();

    [CommandImplementation("moo")]
    public string Moo()
        => "Have you mooed today?";
}
