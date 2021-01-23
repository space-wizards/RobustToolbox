using JetBrains.Annotations;
using Robust.Client.Interfaces.Console;
using Robust.Client.Interfaces.Input;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    [UsedImplicitly]
    public class SetInputContextCommand : IConsoleCommand
    {
        public string Command => "setinputcontext";
        public string Description => "Sets the active input context.";
        public string Help => "setinputcontext <context>";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            if (args.Length != 1)
            {
                console.AddLine("Invalid number of arguments!");
                return false;
            }

            var inputMan = IoCManager.Resolve<IInputManager>();

            if (!inputMan.Contexts.Exists(args[0]))
            {
                console.AddLine("Context not found!");
                return false;
            }

            inputMan.Contexts.SetActiveContext(args[0]);
            return false;
        }
    }
}
