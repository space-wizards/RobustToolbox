using System;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.Console.Commands
{
    [UsedImplicitly]
    internal sealed class AddComponentCommand : LocalizedCommands
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Command => "addcomp";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine($"Invalid amount of arguments.\n{Help}");
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

            if (!_componentFactory.TryGetRegistration(componentName, out var registration, true))
            {
                shell.WriteLine($"No component found with name {componentName}.");
                return;
            }

            if (_entityManager.HasComponent(uid, registration.Type))
            {
                shell.WriteLine($"Entity {_entityManager.GetComponent<MetaDataComponent>(uid).EntityName} already has a {componentName} component.");
            }

            var component = (Component) _componentFactory.GetComponent(registration.Type);

            component.Owner = uid;
            _entityManager.AddComponent(uid, component);

            shell.WriteLine($"Added {componentName} component to entity {_entityManager.GetComponent<MetaDataComponent>(uid).EntityName}.");
        }
    }
}
