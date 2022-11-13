using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    public sealed class SetInputContextCommand : LocalizedCommands
    {
        public override string Command => "setinputcontext";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("Invalid number of arguments!");
                return;
            }

            var inputMan = IoCManager.Resolve<IInputManager>();

            if (!inputMan.Contexts.Exists(args[0]))
            {
                shell.WriteLine("Context not found!");
                return;
            }

            inputMan.Contexts.SetActiveContext(args[0]);
        }
    }
}
