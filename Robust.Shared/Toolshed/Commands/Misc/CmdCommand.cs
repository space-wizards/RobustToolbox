using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
public sealed class CmdCommand : ToolshedCommand
{
    [CommandImplementation("list")]
    public IEnumerable<CommandSpec> List([CommandInvocationContext] IInvocationContext ctx)
        => ctx.Environment.AllCommands();

    [CommandImplementation("moo")]
    public string Moo()
        => "Have you mooed today?";

    [CommandImplementation("descloc")]
    public string GetLocStr([PipedArgument] CommandSpec cmd) => cmd.DescLocStr();

    [CommandImplementation("info")]
    public CommandSpec Info([CommandArgument] CommandSpec cmd) => cmd;

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
