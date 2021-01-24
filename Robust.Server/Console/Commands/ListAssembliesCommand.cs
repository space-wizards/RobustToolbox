using System.Runtime.Loader;
using System.Text;
using JetBrains.Annotations;
using Robust.Server.Interfaces.Player;

namespace Robust.Server.Console.Commands
{
    [UsedImplicitly]
    internal class ListAssembliesCommand : IServerCommand
    {
        public string Command => "lsasm";
        public string Description => "Lists loaded assemblies by load context.";
        public string Help => Command;

        public void Execute(IServerConsoleShell shell, string[] args)
        {
            var sb = new StringBuilder();
            foreach (var context in AssemblyLoadContext.All)
            {
                sb.AppendFormat("{0}:\n", context.Name);
                foreach (var assembly in context.Assemblies)
                {
                    sb.AppendFormat("  {0}\n", assembly.FullName);
                }
            }

            shell.WriteLine(sb.ToString());
        }
    }
}
