using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

namespace Robust.Client.Editor.Testing;

public sealed class EditorDebugCommand : IConsoleCommand
{
    public string Command => "editordebug";

    public string Description => "";

    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        IoCManager.Resolve<IStateManager>().RequestStateChange<EdDockState>();
    }

    [ContentAccessAllowed]
    private sealed class EdDockState : State.State
    {
        [Dependency] private readonly IUserInterfaceManager _uiMgr = default!;

        public override void Startup()
        {
            var control = new EditorDebugControl();
            LayoutContainer.SetAnchorPreset(control, LayoutContainer.LayoutPreset.Wide);
            _uiMgr.StateRoot.AddChild(control);
        }

        public override void Shutdown()
        {
        }
    }
}
