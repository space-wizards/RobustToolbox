using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.MapEditor.Interface;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Client.MapEditor;

[ContentAccessAllowed]
internal sealed class MapEditorState : State.State
{
    [Dependency] private readonly IUserInterfaceManager _uiManager = null!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = null!;
    [Dependency] private readonly IInputManager _inputManager = null!;
    [Dependency] private readonly IOverlayManager _overlayManager = null!;

    private readonly GridBackgroundOverlay _gridBackground = new();

    private readonly MapEditorMain _main;

    public MapEditorState()
    {
        IoCManager.InjectDependencies(this);

        _main = new MapEditorMain(_entitySystem.GetEntitySystem<ClientMapEditorSystem>());
    }

    protected override void Startup()
    {
        _uiManager.StateRoot.AddChild(_main);
        LayoutContainer.SetAnchorAndMarginPreset(_main, LayoutContainer.LayoutPreset.Wide);

        _inputManager.Contexts.SetActiveContext(MapEditorInputContext.ContextName);

        _overlayManager.AddOverlay(_gridBackground);
    }

    protected override void Shutdown()
    {
        _overlayManager.RemoveOverlay<GridBackgroundOverlay>();

        _main.Orphan();
        _main.Shutdown();
    }
}
