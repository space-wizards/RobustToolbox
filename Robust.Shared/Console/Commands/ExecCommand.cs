using System.IO;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Console.Commands
{
    [UsedImplicitly]
    internal sealed class ExecCommand : IConsoleCommand
    {
        private static readonly Regex CommentRegex = new Regex(@"^\s*#");

        public string Command => "exec";
        public string Description => "Executes a script file from the game's data directory.";
        public string Help => "Usage: exec <fileName>\n" +
                              "Each line in the file is executed as a single command, unless it starts with a #";
        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var res = IoCManager.Resolve<IResourceManager>();

            if (args.Length < 1)
            {
                shell.WriteError("No file specified!");
                return;
            }

            var path = new ResourcePath(args[0]).ToRootedPath();
            if (!res.UserData.Exists(path))
            {
                shell.WriteError("File does not exist.");
                return;
            }

            using var text = new StreamReader(res.UserData.OpenRead(path));
            while (true)
            {
                var line = text.ReadLine();
                if (line == null)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(line) || CommentRegex.IsMatch(line))
                {
                    // Comment or whitespace.
                    continue;
                }

                shell.ExecuteCommand(line);
            }
        }
    }
}
