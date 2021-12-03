using System;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Server.Console.Commands
{
    [UsedImplicitly]
    internal sealed class AddComponentCommand : IConsoleCommand
    {
        public string Command => "addcomp";
        public string Description => "Adds a component to an entity";
        public string Help => $"{Command} <uid> <componentName>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
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

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.EntityExists(uid))
            {
                shell.WriteLine($"No entity found with id {uid}.");
                return;
            }

            var componentName = args[1];

            var entManager = IoCManager.Resolve<IEntityManager>();
            var compFactory = IoCManager.Resolve<IComponentFactory>();

            if (!compFactory.TryGetRegistration(componentName, out var registration, true))
            {
                shell.WriteLine($"No component found with name {componentName}.");
                return;
            }

            if (IoCManager.Resolve<IEntityManager>().HasComponent(uid, registration.Type))
            {
                shell.WriteLine($"Entity {entManager.GetComponent<MetaDataComponent>(uid).EntityName} already has a {componentName} component.");
            }

            var component = (Component) compFactory.GetComponent(registration.Type);

            component.Owner = uid;
            entManager.AddComponent(uid, component);

            shell.WriteLine($"Added {componentName} component to entity {entManager.GetComponent<MetaDataComponent>(uid).EntityName}.");
        }
    }
}
