using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Shared.Console.Commands;

public sealed class VfsListCommand : LocalizedCommands
{
    [Dependency] private readonly IResourceManager _resourceManager = default!;

    public override string Command => "vfs_ls";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length > 1)
        {
            shell.WriteError(LocalizationManager.GetString("cmd-vfs_ls-err-args"));
            return;
        }

        var entries = _resourceManager.ContentGetDirectoryEntries(new ResPath(args[0]));
        foreach (var entry in entries)
        {
            shell.WriteLine(entry);
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var opts = CompletionHelper.ContentDirPath(args[0], _resourceManager);
            return CompletionResult.FromHintOptions(opts, LocalizationManager.GetString("cmd-vfs_ls-hint-path"));
        }

        return CompletionResult.Empty;
    }
}
