using System.Linq;
using System.Runtime.Loader;
using System.Text;
using JetBrains.Annotations;
using Robust.Client.Interfaces.Console;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    internal sealed class ListAssembliesCommand : IConsoleCommand
    {
        public string Command => "lsasm";
        public string Description => "Lists loaded assemblies by load context.";
        public string Help => Command;

        public bool Execute(IDebugConsole console, string[] args)
        {
            var sb = new StringBuilder();
            foreach (var context in AssemblyLoadContext.All)
            {
                sb.AppendFormat("{0}:\n", context.Name);
                foreach (var assembly in context.Assemblies.OrderBy(a => a.FullName))
                {
                    sb.AppendFormat("  {0}\n", assembly.FullName);
                }
            }

            console.AddLine(sb.ToString());
            return false;
        }
    }
}
