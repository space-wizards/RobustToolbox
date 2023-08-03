using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
internal sealed class CmdCommand : ToolshedCommand
{
    [CommandImplementation("list")]
    public IEnumerable<CommandSpec> List()
        => Toolshed.AllCommands();

    [CommandImplementation("moo")]
    public string Moo()
        => "Have you mooed today?";

    [CommandImplementation("descloc")]
    public string GetLogStr([PipedArgument] CommandSpec cmd) => cmd.DescLocStr();

#if CLIENT_SCRIPTING
    [CommandImplementation("getshim")]
    public MethodInfo GetShim([CommandArgument] Block block)
    {

        // this is gross sue me
        var invocable = block.CommandRun.Commands.Last().Item1.Invocable;
        return invocable.GetMethodInfo();
    }
#endif
}
