using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.Console.Commands
{
    [UsedImplicitly]
    internal sealed class LookupChunksCommand : IConsoleCommand
    {
        public string Command => "chunkupdates";
        public string Description => "Displays lookup chunks as they are modified";
        public string Help => "chunkupdates <show/hide>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player;
            if (player == null) return;

#if DEBUG
            if (args.Length == 1)
            {
                var eventBus = IoCManager.Resolve<IEntityManager>().EventBus;

                switch (args[0])
                {
                    case "show":
                        eventBus.RaiseEvent(EventSource.Local, new ChunkSubscribeMessage(player));
                        break;
                    case "hide":
                        eventBus.RaiseEvent(EventSource.Local, new ChunkUnsubscribeMessage(player));
                        break;
                    default:
                        shell.WriteLine("Invalid arg");
                        break;
                }
            }
            else
            {
                shell.WriteLine($"Invalid amount of args supplied (need 1 found {args.Length})");
            }
#else
            shell.WriteLine("Command only works in DEBUG");
#endif
        }
    }
}
