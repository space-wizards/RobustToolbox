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
                shell.WriteLine("Wrong number of arguments");
                return;
            }

            var entityUid = EntityUid.Parse(args[0]);
            var componentName = args[1];

            var compManager = IoCManager.Resolve<IComponentManager>();
            var compFactory = IoCManager.Resolve<IComponentFactory>();
            var entityManager = IoCManager.Resolve<IEntityManager>();

            var entity = entityManager.GetEntity(entityUid);

            if (!compFactory.TryGetRegistration(componentName, out var registration, true))
            {
                shell.WriteLine($"No component found with name {componentName}");
                return;
            }

            var component = (Component) compFactory.GetComponent(registration.Type);

            component.Owner = entity;

            compManager.AddComponent(entity, component);
        }
    }
}
