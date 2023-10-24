using Robust.Server.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Player;

namespace Robust.Server.Console.Commands
{
    public sealed class AddViewSubscriberCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entities = default!;

        public override string Command => "addview";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var session = shell.Player;

            if (session is not { } playerSession)
            {
                shell.WriteError($"Unable to find {nameof(ICommonSession)} for shell");
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteError($"Only 1 arg valid for {Command}, received {args.Length}");
                return;
            }

            if (!NetEntity.TryParse(args[0], out var uidNet))
            {
                shell.WriteError($"Unable to parse {args[0]} as a {nameof(EntityUid)}");
                return;
            }

            if (!_entities.TryGetEntity(uidNet, out var uid) || !_entities.EntityExists(uid))
            {
                shell.WriteError($"Unable to find entity {uid}");
                return;
            }

            _entities.EntitySysManager.GetEntitySystem<ViewSubscriberSystem>().AddViewSubscriber(uid.Value, playerSession);
        }

        public sealed class RemoveViewSubscriberCommand : LocalizedCommands
        {
            [Dependency] private readonly IEntityManager _entities = default!;

            public override string Command => "removeview";

            public override void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                var session = shell.Player;

                if (session is not { } playerSession)
                {
                    shell.WriteError($"Unable to find {nameof(ICommonSession)} for shell");
                    return;
                }

                if (args.Length != 1)
                {
                    shell.WriteError($"Only 1 arg valid for {Command}, received {args.Length}");
                    return;
                }

                if (!NetEntity.TryParse(args[0], out var uidNet))
                {
                    shell.WriteError($"Unable to parse {args[0]} as a {nameof(NetEntity)}");
                    return;
                }

                if (!_entities.TryGetEntity(uidNet, out var uid) || !_entities.EntityExists(uid))
                {
                    shell.WriteError($"Unable to find entity {uid}");
                    return;
                }

                _entities.EntitySysManager.GetEntitySystem<ViewSubscriberSystem>().RemoveViewSubscriber(uid.Value, playerSession);
            }
        }
    }
}
