using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    internal sealed class AddCompCommand : IClientCommand
    {
        public string Command => "addcompc";
        public string Description => "Adds a component to an entity on the client";
        public string Help => "addcompc <uid> <componentName>";

        public bool Execute(IClientConsoleShell shell, string argStr, string[] args)
        {

            if (args.Length != 2)
            {
                shell.WriteLine("Wrong number of arguments");
                return false;
            }

            var entityUid = EntityUid.Parse(args[0]);
            var componentName = args[1];

            var compManager = IoCManager.Resolve<IComponentManager>();
            var compFactory = IoCManager.Resolve<IComponentFactory>();
            var entityManager = IoCManager.Resolve<IEntityManager>();

            var entity = entityManager.GetEntity(entityUid);
            var component = (Component) compFactory.GetComponent(componentName);

            component.Owner = entity;

            compManager.AddComponent(entity, component);

            return false;
        }
    }

    [UsedImplicitly]
    internal sealed class RemoveCompCommand : IClientCommand
    {
        public string Command => "rmcompc";
        public string Description => "Removes a component from an entity.";
        public string Help => "rmcompc <uid> <componentName>";

        public bool Execute(IClientConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine("Wrong number of arguments");
                return false;
            }

            var entityUid = EntityUid.Parse(args[0]);
            var componentName = args[1];

            var compManager = IoCManager.Resolve<IComponentManager>();
            var compFactory = IoCManager.Resolve<IComponentFactory>();

            var registration = compFactory.GetRegistration(componentName);

            compManager.RemoveComponent(entityUid, registration.Type);

            return false;
        }
    }
}
