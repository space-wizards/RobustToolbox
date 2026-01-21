using System.IO;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.MapEditor;
using Robust.Shared.Utility;

namespace Robust.Client.MapEditor;

internal sealed class MapEditorCommand : IConsoleCommand
{
    [Dependency] private readonly IBaseClient _baseClient = default!;
    [Dependency] private readonly MapEditorPrimer _primer = default!;
    [Dependency] private readonly IEntitySystemManager _esm = null!;

    public string Command => "mapeditor";
    public string Description => "";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_baseClient.RunLevel != ClientRunLevel.InGame)
        {
            _primer.Prime();
            shell.WriteLine("Not connected to server yet. Will now attempt to launch map editor as soon as we are connected.");
            return;
        }

        _esm.GetEntitySystem<ClientMapEditorSystem>().RequestStartEditing();
    }
}

internal abstract class MapEditorBaseCommand : LocalizedEntityCommands
{
    [Dependency] protected readonly ClientMapEditorSystem MapEditor = null!;
}

internal sealed class MapEditorOpenFileCommand : MapEditorBaseCommand
{
    [Dependency] private readonly IResourceManager _res = null!;
    [Dependency] private readonly MapFileHandleManager _handleManager = null!;

    public override string Command => "mapeditor_openfile";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var path = new ResPath(args[0]);
        var stream = _res.UserData.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        var handle = _handleManager.CreateHandleForExistingStream(stream);

        MapEditor.RequestOpenFile(path.FilenameWithoutExtension, handle);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromOptions(CompletionHelper.UserFilePath(args[0], _res.UserData));

        return CompletionResult.Empty;
    }
}
