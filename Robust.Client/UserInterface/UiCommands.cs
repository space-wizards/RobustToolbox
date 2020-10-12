using System;
using System.Collections.Generic;
using System.Text;
using Robust.Client.Interfaces.Console;
using Robust.Client.Interfaces.State;
using Robust.Client.Interfaces.UserInterface;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Robust.Client.UserInterface
{
    class ChangeSceneCommand : IConsoleCommand
    {
        public string Command => "scene";
        public string Help => "scene <className>";
        public string Description => "Immediately changes the UI scene/state.";

        public bool Execute(IDebugConsole console, params string[] args)
        {
            var reflection = IoCManager.Resolve<IReflectionManager>();
            var type = reflection.LooseGetType(args[0]);

            var stateMan = IoCManager.Resolve<IStateManager>();

            stateMan.RequestStateChange(type);

            return false;
        }
    }
}
