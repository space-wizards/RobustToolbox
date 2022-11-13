using System;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.Console.Commands
{
    sealed class HardQuitCommand : LocalizedCommands
    {
        public override string Command => "hardquit";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            Environment.Exit(0);
        }
    }

    sealed class QuitCommand : LocalizedCommands
    {
        public override string Command => "quit";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            IoCManager.Resolve<IGameController>().Shutdown("quit command used");
        }
    }
}
