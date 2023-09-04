using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    [InjectDependencies]
    internal sealed partial class AddCompCommand : LocalizedCommands
    {
        [Dependency] private IComponentFactory _componentFactory = default!;
        [Dependency] private IEntityManager _entityManager = default!;

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

            var component = (Component) _componentFactory.GetComponent(componentName);

            component.Owner = entity;

            _entityManager.AddComponent(entity, component);
        }
    }

    [UsedImplicitly]
    [InjectDependencies]
    internal sealed partial class RemoveCompCommand : LocalizedCommands
    {
        [Dependency] private IComponentFactory _componentFactory = default!;
        [Dependency] private IEntityManager _entityManager = default!;

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

            var registration = _componentFactory.GetRegistration(componentName);

            _entityManager.RemoveComponent(entityUid, registration.Type);
        }
    }
}
