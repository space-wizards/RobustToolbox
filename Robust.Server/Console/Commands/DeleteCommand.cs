using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.Console.Commands
{
    public sealed class DeleteCommand : LocalizedCommands
    {
        public override string Command => "delete";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("You should provide exactly one argument.");
                return;
            }

            var ent = IoCManager.Resolve<IServerEntityManager>();

            if (!EntityUid.TryParse(args[0], out var entity))
            {
                shell.WriteLine("Invalid entity UID.");
                return;
            }

            if (!ent.EntityExists(entity))
            {
                shell.WriteLine("That entity does not exist.");
                return;
            }

            ent.DeleteEntity(entity);
        }
    }
}
