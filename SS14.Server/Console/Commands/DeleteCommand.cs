using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.IoC;

namespace SS14.Server.Console.Commands
{
    public sealed class DeleteCommand : IClientCommand
    {
        public string Command => "delete";
        public string Description => "Deletes the entity with the specified ID.";
        public string Help => "delete <entity UID>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            if (args.Length != 1)
            {
                shell.SendText(player, "You should provide exactly one argument.");
                return;
            }

            var ent = IoCManager.Resolve<IServerEntityManager>();

            if (!EntityUid.TryParse(args[0], out var uid))
            {
                shell.SendText(player, "Invalid entity UID.");
                return;
            }

            if (!ent.TryGetEntity(uid, out var entity))
            {
                shell.SendText(player, "That entity does not exist.");
                return;
            }

            entity.Delete();
        }
    }
}
