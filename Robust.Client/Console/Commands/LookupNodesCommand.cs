using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Interfaces.Console;
using Robust.Shared.GameObjects.Systems;

namespace Robust.Client.Console.Commands
{
    public class LookupNodesCommand : IConsoleCommand
    {
        public string Command => "lookupnodes";
        public string Description => "Shows the entities on each client-side lookup node";
        public string Help => "lookupnodes <show/hide>";
        public bool Execute(IDebugConsole console, params string[] args)
        {
#if DEBUG
            if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "show":
                        EntitySystem.Get<EntityLookupSystem>().DebugNodes = true;
                        return false;
                    case "hide":
                        EntitySystem.Get<EntityLookupSystem>().DebugNodes = false;
                        return false;
                    default:
                        console.AddLine("Invalid arg");
                        return true;
                }
            }
            else
            {
                console.AddLine($"Invalid amount of args supplied (need 1 found {args.Length})");
                return true;
            }
#else
            console.AddLine("Command only works in DEBUG");
            return true;
#endif
        }
    }
}
