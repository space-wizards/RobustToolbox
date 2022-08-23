using Robust.Client.State;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Robust.Client.UserInterface
{
    sealed class ChangeSceneCommpand : IConsoleCommand
    {
        public string Command => "scene";
        public string Help => "scene <className>";
        public string Description => "Immediately changes the UI scene/state.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var reflection = IoCManager.Resolve<IReflectionManager>();
            var types = reflection.GetAllChildren(typeof(State.State));

            foreach (var tryType in types)
            {
                if (tryType.FullName!.EndsWith(args[0]))
                {
                    var stateMan = IoCManager.Resolve<IStateManager>();
                    stateMan.RequestStateChange(tryType);
                    shell.WriteLine($"Switching to scene {tryType.FullName}");
                    return;
                }
            }

            shell.WriteError($"No scene child class type ends with {args[0]}");
        }
    }
}
