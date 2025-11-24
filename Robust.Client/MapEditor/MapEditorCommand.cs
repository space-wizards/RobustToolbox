using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

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
