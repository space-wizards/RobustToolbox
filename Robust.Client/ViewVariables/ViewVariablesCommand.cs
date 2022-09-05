using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Maths;
using Robust.Shared.Reflection;
using Robust.Shared.ViewVariables;

namespace Robust.Client.ViewVariables
{
    [UsedImplicitly]
    public sealed class ViewVariablesCommand : IConsoleCommand
    {
        [Dependency] private readonly IClientViewVariablesManager _vvm = default!;

        public string Command => "vv";
        public string Description => "Opens View Variables.";
        public string Help => "Usage: vv <entity ID|IoC interface name|SIoC interface name>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            // If you don't provide an entity ID, it opens the test class.
            // Spooky huh.
            if (args.Length == 0)
            {
                _vvm.OpenVV(new ViewVariablesPathSelector("/vvtest"));
                return;
            }

            var valArg = string.Join(string.Empty, args);

            if (valArg.StartsWith("/"))
            {
                var selector = new ViewVariablesPathSelector(valArg);
                _vvm.OpenVV(selector);
                return;
            }

            if (valArg.StartsWith("SI"))
            {
                if (valArg.StartsWith("SIoC"))
                    valArg = valArg.Substring(4);

                // Server-side IoC selector.
                var selector = new ViewVariablesIoCSelector(valArg.Substring(1));
                _vvm.OpenVV(selector);
                return;
            }

            if (valArg.StartsWith("I"))
            {
                if (valArg.StartsWith("IoC"))
                    valArg = valArg.Substring(3);

                // Client-side IoC selector.
                var reflection = IoCManager.Resolve<IReflectionManager>();
                if (!reflection.TryLooseGetType(valArg, out var type))
                {
                    shell.WriteLine("Unable to find that type.");
                    return;
                }

                object obj;
                try
                {
                    obj = IoCManager.ResolveType(type);
                }
                catch (UnregisteredTypeException)
                {
                    shell.WriteLine("Unable to find that type.");
                    return;
                }
                _vvm.OpenVV(obj);
                return;
            }

            // Client side entity system.
            if (valArg.StartsWith("CE"))
            {
                valArg = valArg.Substring(2);
                var reflection = IoCManager.Resolve<IReflectionManager>();

                if (!reflection.TryLooseGetType(valArg, out var type))
                {
                    shell.WriteLine("Unable to find that type.");
                    return;
                }

                _vvm.OpenVV(IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem(type));
            }

            if (valArg.StartsWith("SE"))
            {
                // Server-side Entity system selector.
                var selector = new ViewVariablesEntitySystemSelector(valArg.Substring(2));
                _vvm.OpenVV(selector);
                return;
            }

            if (valArg.StartsWith("guihover"))
            {
                // UI element.
                var obj = IoCManager.Resolve<IUserInterfaceManager>().CurrentlyHovered;
                if (obj == null)
                {
                    shell.WriteLine("Not currently hovering any control.");
                    return;
                }
                _vvm.OpenVV(obj);
                return;
            }

            // Entity.
            if (!EntityUid.TryParse(args[0], out var entity))
            {
                shell.WriteLine("Invalid specifier format.");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();
            if (!entityManager.EntityExists(entity))
            {
                shell.WriteLine("That entity does not exist locally. Attempting to open remote view...");
                _vvm.OpenVV(new ViewVariablesEntitySelector(entity));
                return;
            }

            _vvm.OpenVV(entity);
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            return CompletionResult.FromOptions(
                _vvm.ListPath(string.Join(string.Empty, args))
                    .Select(p => new CompletionOption(p, null, CompletionOptionFlags.PartialCompletion)));
        }
    }
}
