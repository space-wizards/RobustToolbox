using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Robust.Shared.Console.Commands
{
    [UsedImplicitly]
    internal sealed class ExecCommand : LocalizedCommands
    {
        private static readonly Regex CommentRegex = new Regex(@"^\s*#");

        [Dependency] private readonly IResourceManager _resources = default!;

        public override string Command => "exec";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteError("No file specified!");
                return;
            }

            var path = new ResourcePath(args[0]).ToRootedPath();
            if (!_resources.UserData.Exists(path))
            {
                shell.WriteError("File does not exist.");
                return;
            }

            using var text = _resources.UserData.OpenText(path);
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

                shell.ConsoleHost.AppendCommand(line);
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                var hint = Loc.GetString("cmd-exec-arg-filename");
                var options = CompletionHelper.UserFilePath(args[0], _resources.UserData);

                return CompletionResult.FromHintOptions(options, hint);
            }

            return CompletionResult.Empty;
        }
    }
}
