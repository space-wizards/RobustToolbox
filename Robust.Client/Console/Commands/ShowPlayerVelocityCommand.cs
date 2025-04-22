using Robust.Client.GameObjects;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    public sealed class ShowPlayerVelocityCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly ShowPlayerVelocityDebugSystem _system = default!;

        public override string Command => "showplayervelocity";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _system.Enabled ^= true;
        }
    }
}
