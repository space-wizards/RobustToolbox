using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    internal sealed class AddCompCommand : LocalizedCommands
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Command => "addcompc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {

            if (args.Length != 2)
            {
                shell.WriteLine("Wrong number of arguments");
                return;
            }

            var netEntity = NetEntity.Parse(args[0]);
            var entity = _entityManager.GetEntity(netEntity);
            var componentName = args[1];

            var component = _componentFactory.GetComponent(componentName);
            _entityManager.AddComponent(entity, component);
        }
    }

    [UsedImplicitly]
    internal sealed class RemoveCompCommand : LocalizedCommands
    {
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Command => "rmcompc";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 2)
            {
                shell.WriteLine("Wrong number of arguments");
                return;
            }

            var netEntity = NetEntity.Parse(args[0]);
            var entityUid = _entityManager.GetEntity(netEntity);
            var componentName = args[1];

            var registration = _componentFactory.GetRegistration(componentName);

            _entityManager.RemoveComponent(entityUid, registration.Type);
        }
    }
}
