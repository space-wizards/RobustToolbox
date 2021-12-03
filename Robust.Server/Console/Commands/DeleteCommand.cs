using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.Console.Commands
{
    public sealed class DeleteCommand : IConsoleCommand
    {
        public string Command => "delete";
        public string Description => "Deletes the entity with the specified ID.";
        public string Help => "delete <entity UID>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("You should provide exactly one argument.");
                return;
            }

            var ent = IoCManager.Resolve<IServerEntityManager>();

            if (!EntityUid.TryParse(args[0], out var uid))
            {
                shell.WriteLine("Invalid entity UID.");
                return;
            }

            if (!ent.TryGetEntity(uid, out var entity))
            {
                shell.WriteLine("That entity does not exist.");
                return;
            }

            IoCManager.Resolve<IEntityManager>().DeleteEntity(entity.Uid);
        }
    }
}
