using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    public sealed class SetInputContextCommand : LocalizedCommands
    {
        [Dependency] private readonly IInputManager _inputManager = default!;

        public override string Command => "setinputcontext";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length != 1)
            {
                shell.WriteLine("Invalid number of arguments!");
                return;
            }

            if (!_inputManager.Contexts.Exists(args[0]))
            {
                shell.WriteLine("Context not found!");
                return;
            }

            _inputManager.Contexts.SetActiveContext(args[0]);
        }
    }
}
