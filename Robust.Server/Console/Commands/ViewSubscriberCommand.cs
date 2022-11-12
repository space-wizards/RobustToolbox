using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;

namespace Robust.Server.Console.Commands
{
    public sealed class AddViewSubscriberCommand : LocalizedCommands
    {
        public override string Command => "addview";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var session = shell.Player;

            if (session is not IPlayerSession playerSession)
            {
                shell.WriteError($"Unable to find {nameof(ICommonSession)} for shell");
                return;
            }

            if (args.Length != 1)
            {
                shell.WriteError($"Only 1 arg valid for {Command}, received {args.Length}");
                return;
            }

            if (!EntityUid.TryParse(args[0], out var uid))
            {
                shell.WriteError($"Unable to parse {args[0]} as a {nameof(EntityUid)}");
                return;
            }

            var entManager = IoCManager.Resolve<IEntityManager>();

            if (!entManager.EntityExists(uid))
            {
                shell.WriteError($"Unable to find entity {uid}");
                return;
            }

            EntitySystem.Get<ViewSubscriberSystem>().AddViewSubscriber(uid, playerSession);
        }

        public sealed class RemoveViewSubscriberCommand : LocalizedCommands
        {
            public override string Command => "removeview";

            public override void Execute(IConsoleShell shell, string argStr, string[] args)
            {
                var session = shell.Player;

                if (session is not IPlayerSession playerSession)
                {
                    shell.WriteError($"Unable to find {nameof(ICommonSession)} for shell");
                    return;
                }

                if (args.Length != 1)
                {
                    shell.WriteError($"Only 1 arg valid for {Command}, received {args.Length}");
                    return;
                }

                if (!EntityUid.TryParse(args[0], out var uid))
                {
                    shell.WriteError($"Unable to parse {args[0]} as a {nameof(EntityUid)}");
                    return;
                }

                var entManager = IoCManager.Resolve<IEntityManager>();

                if (!entManager.EntityExists(uid))
                {
                    shell.WriteError($"Unable to find entity {uid}");
                    return;
                }

                EntitySystem.Get<ViewSubscriberSystem>().RemoveViewSubscriber(uid, playerSession);
            }
        }
    }
}
