using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    internal sealed class ClientSpawnCommand : LocalizedCommands
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Command => "cspawn";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (_playerManager.LocalEntity is not { } controlled)
            {
                shell.WriteLine("You don't have an attached entity.");
                return;
            }

            var entityManager = _entityManager;
            entityManager.SpawnEntity(args[0], entityManager.GetComponent<TransformComponent>(controlled).Coordinates);
        }
    }
}
