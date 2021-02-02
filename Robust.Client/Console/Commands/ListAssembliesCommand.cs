using System.Linq;
using System.Runtime.Loader;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.Console;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    internal sealed class ListAssembliesCommand : IConsoleCommand
    {
        public string Command => "lsasm";
        public string Description => "Lists loaded assemblies by load context.";
        public string Help => Command;

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            foreach (var context in AssemblyLoadContext.All)
            {
                shell.WriteLine($"{context.Name}:");
                foreach (var assembly in context.Assemblies.OrderBy(a => a.FullName))
                {
                    shell.WriteLine($"  {assembly.FullName}");
                }
            }
        }
    }
}
