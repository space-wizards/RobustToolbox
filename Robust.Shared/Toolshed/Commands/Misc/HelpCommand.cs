using Robust.Shared.Maths;

namespace Robust.Shared.Toolshed.Commands.Misc;

[ToolshedCommand]
public sealed class HelpCommand : ToolshedCommand
{
    private static readonly string Gold = Color.Gold.ToHex();
    private static readonly string Aqua = Color.Aqua.ToHex();

    [CommandImplementation]
    public void Help([CommandInvocationContext] IInvocationContext ctx)
    {
        ctx.WriteLine($@"
  TOOLSHED
 /.\\\\\\\\
/___\\\\\\\\
|''''|'''''|
| 8  | === |
|_0__|_____|");
        ctx.WriteMarkup($@"
For a list of commands, run [color={Gold}]cmd:list[/color].
To search for commands, run [color={Gold}]cmd:list search ""[color={Aqua}]query[/color]""[/color].
For a breakdown of how a string of commands flows, run [color={Gold}]explain [color={Aqua}]commands here[/color][/color].
For help with old console commands, run [color={Gold}]oldhelp[/color].
");
    }
}
