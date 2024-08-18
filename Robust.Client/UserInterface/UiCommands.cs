using System.Linq;
using Robust.Client.State;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;

namespace Robust.Client.UserInterface
{
    sealed class ChangeSceneCommpand : LocalizedCommands
    {
        [Dependency] private readonly IReflectionManager _reflection = default!;

        public override string Command => "scene";
        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var types = _reflection.GetAllChildren(typeof(State.State));

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

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length != 1)
                return CompletionResult.Empty;
            var types = _reflection.GetAllChildren(typeof(State.State));
            return CompletionResult.FromHintOptions(types.Select(x => x.Name), "[State name]");
        }
    }
}
