using Robust.Shared.Reflection;

namespace Robust.Shared.Console
{
    [Reflect(false)]
    public class RegisteredCommand : IConsoleCommand
    {
        private readonly ConCommandCallback _callback;

        public string Command { get; }
        public string Description { get; }
        public string Help { get; }

        public RegisteredCommand(string command, string description, string help, ConCommandCallback callback)
        {
            Command = command;
            Description = description;
            Help = help;
            _callback = callback;
        }

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            _callback(shell, argStr, args);
        }
    }
}
