using Robust.Client.GameStates;
using Robust.Client.Interfaces.Console;
using Robust.Client.Interfaces.Graphics;
using Robust.Client.Interfaces.Graphics.Overlays;
using Robust.Shared.IoC;

namespace Robust.Client.Graphics.Clyde
{
    class ReloadShadersCommand : IConsoleCommand
    {
        public string Command => "reload_shaders";
        public string Help => "reload_shaders";
        public string Description => "Reload shaders and reinitialize lighting.";

        public bool Execute(IDebugConsole console, params string[] args)
        {

            console.AddLine("Reloading shaders and reinitializing lighting...");
            IoCManager.Resolve<IClyde>().ReloadShaders();
            console.AddLine("Done.");

            return false;
        }
    }

}
