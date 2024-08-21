using System.IO.Compression;
using System.Linq;
using JetBrains.Annotations;
using Robust.Client.Replays.Loading;
using Robust.Client.Replays.Playback;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Robust.Client.Replays.Commands;

[UsedImplicitly]
public sealed class ReplayLoadCommand : BaseReplayCommand
{
    [Dependency] private readonly IResourceManager _resMan = default!;
    [Dependency] private readonly IReplayLoadManager _loadMan = default!;
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override string Command => IReplayPlaybackManager.LoadCommand;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (PlaybackManager.Replay != null)
        {
            shell.WriteError(LocalizationManager.GetString("cmd-replay-error-already-loaded"));
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-invalid-arg-number-error"));
            return;
        }

        if (_client.RunLevel != ClientRunLevel.Initialize && _client.RunLevel != ClientRunLevel.SinglePlayerGame)
        {
            shell.WriteError(Loc.GetString("cmd-replay-error-run-level"));
            return;
        }

        var file = new ResPath(_cfg.GetCVar(CVars.ReplayDirectory)) / args[0];
        if (!_resMan.UserData.Exists(file))
        {
            shell.WriteError(Loc.GetString("cmd-error-file-not-found", ("file", file)));
            return;
        }

        var stream = _resMan.UserData.OpenRead(file);
        var provider = new ReplayFileReaderZip(new ZipArchive(stream), ReplayConstants.ReplayZipFolder);

        _loadMan.LoadAndStartReplay(provider);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var dir = new ResPath(_cfg.GetCVar(CVars.ReplayDirectory)) / args[0];
        dir = dir.ToRootedPath();
        var opts = CompletionHelper.UserFilePath(dir.CanonPath, _resMan.UserData);

        return CompletionResult.FromHintOptions(opts, Loc.GetString("cmd-replay-load-hint"));
    }
}

