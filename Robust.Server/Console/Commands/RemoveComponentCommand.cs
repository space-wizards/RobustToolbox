using System;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.Console.Commands
{
    [UsedImplicitly]
    internal sealed class RemoveComponentCommand : LocalizedCommands
    {
        [Dependency] private readonly IComponentFactory _compFactory = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Command => "rmcomp";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine($"Invalid amount of arguments.\n{Help}.");
                return;
            }

            if (!EntityUid.TryParse(args[0], out var uid))
            {
                shell.WriteLine($"{uid} is not a valid entity uid.");
                return;
            }

            if (!_entityManager.EntityExists(uid))
            {
                shell.WriteLine($"No entity found with id {uid}.");
                return;
            }

            var componentName = args[1];

            if (!_compFactory.TryGetRegistration(componentName, out var registration, true))
            {
                shell.WriteLine($"No component found with name {componentName}.");
                return;
            }

            if (!_entityManager.HasComponent(uid, registration.Type))
            {
                shell.WriteLine($"No {componentName} component found on entity {_entityManager.GetComponent<MetaDataComponent>(uid).EntityName}.");
                return;
            }

            _entityManager.RemoveComponent(uid, registration.Type);

            shell.WriteLine($"Removed {componentName} component from entity {_entityManager.GetComponent<MetaDataComponent>(uid).EntityName}.");
        }
    }
}
