using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.State.States;
using SS14.Client.UserInterface.Components;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.CustomControls
{
    /// <summary>
    ///     The main menu UI window that opens up when ESC is pressed while ingame.
    /// </summary>
    internal class MenuWindow : Window
    {
        private readonly IClientNetManager _netMgr = IoCManager.Resolve<IClientNetManager>();
        private readonly IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();

        public MenuWindow() : base("MainMenu", new Vector2i(140, 130))
        {
            _screenPos = new Vector2i((int) (CluwneLib.CurrentRenderTarget.Size.X / 2f) - (int) (ClientArea.Width / 2f),
                (int) (CluwneLib.CurrentRenderTarget.Size.Y / 2f) - (int) (ClientArea.Height / 2f));

            var buttonEntity = new Button("Spawn Entities");
            buttonEntity.LocalPosition = new Vector2i(5, 5);
            Container.AddControl(buttonEntity);
            buttonEntity.Clicked += button_entity_Clicked;

            var buttonTile = new Button("Spawn Tiles");
            buttonTile.LocalPosition = new Vector2i(0, 5);
            buttonTile.Alignment = Align.Bottom;
            buttonEntity.AddControl(buttonTile);
            buttonTile.Clicked += button_tile_Clicked;

            var buttonQuit = new Button("Quit");
            buttonQuit.LocalPosition = new Vector2i(0, 20);
            buttonQuit.Alignment = Align.Bottom;
            buttonTile.AddControl(buttonQuit);
            buttonQuit.Clicked += button_quit_Clicked;
        }

        protected override void CloseButtonClicked(ImageButton sender)
        {
            Visible = !Visible;
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (e.Key == Keyboard.Key.Escape)
                if (Visible)
                {
                    Visible = false;
                    return true;
                }
            return false;
        }

        private void button_quit_Clicked(Button sender)
        {
            _netMgr.ClientDisconnect("Client disconnected from game.");
            IoCManager.Resolve<IStateManager>().RequestStateChange<MainScreen>();
            Dispose();
        }

        private void button_tile_Clicked(Button sender)
        {
            _userInterfaceManager.DisposeAllComponents<TileSpawnWindow>(); //Remove old ones.

            var tileSpawnPanel = new TileSpawnWindow(new Vector2i(350, 410));
            _userInterfaceManager.AddComponent(tileSpawnPanel);
            tileSpawnPanel.DoLayout();

            // hide me
            Visible = !Visible;
            _userInterfaceManager.RemoveFocus(this);
        }

        private void button_entity_Clicked(Button sender)
        {
            _userInterfaceManager.DisposeAllComponents<EntitySpawnWindow>(); //Remove old ones.
            _userInterfaceManager.AddComponent(new EntitySpawnWindow(new Vector2i(350, 410)));
            //Create a new one.
            Visible = !Visible;
            _userInterfaceManager.RemoveFocus(this);
        }
    }
}
