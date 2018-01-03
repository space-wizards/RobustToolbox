using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.State.States;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;

namespace SS14.Client.UserInterface
{
    public class EscapeMenu : SS14Window
    {
        [Dependency]
        readonly IClientNetManager netManager;
        [Dependency]
        readonly IStateManager stateManager;

        protected override Godot.Control SpawnSceneControl()
        {
            var res = (Godot.PackedScene)Godot.ResourceLoader.Load("res://Scenes/EscapeMenu/EscapeMenu.tscn");
            return (Godot.Control)res.Instance();
        }

        private BaseButton QuitButton;

        protected override void Initialize()
        {
            base.Initialize();

            Resizable = false;
            HideOnClose = true;

            IoCManager.InjectDependencies(this);

            QuitButton = Contents.GetChild<BaseButton>("QuitButton");
            QuitButton.OnPressed += OnQuitButtonClicked;
        }

        private void OnQuitButtonClicked(BaseButton.ButtonEventArgs args)
        {
            netManager.ClientDisconnect("Client disconnected from game.");
            stateManager.RequestStateChange<MainScreen>();
            Dispose();
        }
    }
}
