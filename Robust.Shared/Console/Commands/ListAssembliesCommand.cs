using System.Runtime.Loader;
using System.Text;
using Robust.Shared.Localization;

namespace Robust.Shared.Console.Commands;

internal sealed class ListAssembliesCommand : IConsoleCommand
{
    public string Command => "lsasm";
    public string Description => Loc.GetString("cmd-lsasm-desc");
    public string Help => Loc.GetString("cmd-lsasm-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var sb = new StringBuilder();
        foreach (var context in AssemblyLoadContext.All)
        {
            sb.Append($"{context.Name}:\n");
            foreach (var assembly in context.Assemblies)
            {
                sb.Append($"  {assembly.FullName}\n");
            }
        }

        shell.WriteLine(sb.ToString());
    }
}
