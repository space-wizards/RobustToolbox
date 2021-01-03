using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.Console.Commands
{
    [UsedImplicitly]
    internal sealed class LookupChunksCommand : IClientCommand
    {
        public string Command => "chunkupdates";
        public string Description => "Displays lookup chunks as they are modified";
        public string Help => "chunkupdates <show/hide>";
        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
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
                        shell.SendText(player, "Invalid arg");
                        break;
                }
            }
            else
            {
                shell.SendText(player, $"Invalid amount of args supplied (need 1 found {args.Length})");
            }
#else
            shell.SendText(player, "Command only works in DEBUG");
#endif
        }
    }
}
