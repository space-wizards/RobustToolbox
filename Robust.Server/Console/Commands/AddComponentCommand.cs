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

            if (!NetEntity.TryParse(args[0], out var uidNet))
            {
                shell.WriteLine($"{args[0]} is not a valid entity.");
                return;
            }

            if (!_entityManager.TryGetEntity(uidNet, out var uid) || !_entityManager.EntityExists(uid))
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
                shell.WriteLine($"Entity {_entityManager.GetComponent<MetaDataComponent>(uid.Value).EntityName} already has a {componentName} component.");
            }

            var component = _componentFactory.GetComponent(registration.Type);
            _entityManager.AddComponent(uid.Value, component);

            shell.WriteLine($"Added {componentName} component to entity {_entityManager.GetComponent<MetaDataComponent>(uid.Value).EntityName}.");
        }
    }
}
