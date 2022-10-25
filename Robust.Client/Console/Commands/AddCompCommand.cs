using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    internal sealed class AddCompCommand : LocalizedCommands
    {
        public override string Command => "addcompc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {

            if (args.Length != 2)
            {
                shell.WriteLine("Wrong number of arguments");
                return;
            }

            var entity = EntityUid.Parse(args[0]);
            var componentName = args[1];

            var compFactory = IoCManager.Resolve<IComponentFactory>();
            var entityManager = IoCManager.Resolve<IEntityManager>();

            var component = (Component) compFactory.GetComponent(componentName);

            component.Owner = entity;

            entityManager.AddComponent(entity, component);
        }
    }

    [UsedImplicitly]
    internal sealed class RemoveCompCommand : LocalizedCommands
    {
        public override string Command => "rmcompc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine("Wrong number of arguments");
                return;
            }

            var entityUid = EntityUid.Parse(args[0]);
            var componentName = args[1];

            var entManager = IoCManager.Resolve<IEntityManager>();
            var compFactory = IoCManager.Resolve<IComponentFactory>();

            var registration = compFactory.GetRegistration(componentName);

            entManager.RemoveComponent(entityUid, registration.Type);
        }
    }
}
