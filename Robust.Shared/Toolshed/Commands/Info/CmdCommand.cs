using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Robust.Shared.Toolshed.Syntax;
using Invocable = System.Func<Robust.Shared.Toolshed.CommandInvocationArguments, object?>;

namespace Robust.Shared.Toolshed.Commands.Info;

[ToolshedCommand]
internal sealed class CmdCommand : ToolshedCommand
{
    [CommandImplementation("list")]
    public IEnumerable<CommandSpec> List()
        => Toolshed.AllCommands();

    [CommandImplementation("moo")]
    public string Moo()
        => "Have you mooed today?";

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
