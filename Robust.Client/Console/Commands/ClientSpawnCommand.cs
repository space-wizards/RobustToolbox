using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    internal sealed class ClientSpawnCommand : IClientCommand
    {
        public string Command => "cspawn";
        public string Description => "Spawns a client-side entity with specific type at your feet.";
        public string Help => "cspawn <entity type>";

        public bool Execute(IClientConsoleShell shell, string[] args)
        {
            var player = IoCManager.Resolve<IPlayerManager>().LocalPlayer;
            if (player?.ControlledEntity == null)
            {
                shell.WriteLine("You don't have an attached entity.");
                return false;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            entityManager.SpawnEntity(args[0], player.ControlledEntity.Transform.Coordinates);
            return false;
        }
    }
}
