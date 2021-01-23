using System.Linq;
using System.Runtime.Loader;
using System.Text;
using JetBrains.Annotations;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    internal sealed class ListAssembliesCommand : IClientCommand
    {
        public string Command => "lsasm";
        public string Description => "Lists loaded assemblies by load context.";
        public string Help => Command;

        public bool Execute(IClientConsoleShell shell, string[] args)
        {
            foreach (var context in AssemblyLoadContext.All)
            {
                shell.WriteLine($"{context.Name}:");
                foreach (var assembly in context.Assemblies.OrderBy(a => a.FullName))
                {
                    shell.WriteLine($"  {assembly.FullName}");
                }
            }

            return false;
        }
    }
}
