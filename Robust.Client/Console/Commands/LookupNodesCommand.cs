using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;

namespace Robust.Client.Console.Commands
{
    public class LookupNodesCommand : IConsoleCommand
    {
        public string Command => "lookupnodes";
        public string Description => "Shows the entities on each client-side lookup node";
        public string Help => "lookupnodes <show/hide>";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
#if DEBUG
            if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "show":
                        EntitySystem.Get<EntityLookupSystem>().DebugNodes = true;
                        return;
                    case "hide":
                        EntitySystem.Get<EntityLookupSystem>().DebugNodes = false;
                        return;
                    default:
                        shell.WriteLine("Invalid arg");
                        return;
                }
            }

            shell.WriteLine($"Invalid amount of args supplied (need 1 found {args.Length})");
#else
            shell.WriteLine("Command only works in DEBUG");
            return;
#endif
        }
    }
}
