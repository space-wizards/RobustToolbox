using Robust.Client.State;
using Robust.Shared.Console;
using Robust.Shared.IoC;

namespace Robust.Client.MapEditor;

internal sealed class MapEditorCommand : IConsoleCommand
{
    [Dependency] private readonly IStateManager _stateManager = null!;

    public string Command => "mapeditor";
    public string Description => "";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        _stateManager.RequestStateChange<MapEditorState>();
    }
}
