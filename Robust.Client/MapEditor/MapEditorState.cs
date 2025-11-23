using Robust.Client.MapEditor.Interface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

namespace Robust.Client.MapEditor;

[ContentAccessAllowed]
internal sealed class MapEditorState : State.State
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = null!;

    private readonly MapEditorMain _main = new();

    protected override void Startup()
    {
        _uiManager.StateRoot.AddChild(_main);
        LayoutContainer.SetAnchorAndMarginPreset(_main, LayoutContainer.LayoutPreset.Wide);
    }

    protected override void Shutdown()
    {
        _main.Orphan();
    }
}
