using System;
using System.Collections.Generic;
using System.Text;
using Robust.Client.State;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Reflection;

namespace Robust.Client.UserInterface
{
    class ChangeSceneCommpand : IConsoleCommand
    {
        public string Command => "scene";
        public string Help => "scene <className>";
        public string Description => "Immediately changes the UI scene/state.";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var reflection = IoCManager.Resolve<IReflectionManager>();
            var type = reflection.LooseGetType(args[0]);

            var stateMan = IoCManager.Resolve<IStateManager>();

            stateMan.RequestStateChange(type);
        }
    }
}
