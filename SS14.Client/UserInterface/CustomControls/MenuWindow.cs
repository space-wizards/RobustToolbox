using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces.State;
using SS14.Client.Interfaces.UserInterface;
using SS14.Client.State.States;
using SS14.Client.UserInterface.Components;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.CustomControls
{
    /// <summary>
    /// The main menu UI window that opens up when ESC is pressed while ingame.
    /// </summary>
    internal class MenuWindow : Window
    {
        private readonly IClientNetManager _netMgr = IoCManager.Resolve<IClientNetManager>();
        private readonly IUserInterfaceManager _userInterfaceManager = IoCManager.Resolve<IUserInterfaceManager>();

        private readonly Button button_entity;
        private readonly Button button_quit;
        private readonly Button button_tile;

        public MenuWindow() : base("MainMenu", new Vector2i(140, 130))
        {
            _screenPos = new Vector2i((int)(CluwneLib.CurrentRenderTarget.Size.X / 2f) - (int)(ClientArea.Width / 2f),
                                 (int)(CluwneLib.CurrentRenderTarget.Size.Y / 2f) - (int)(ClientArea.Height / 2f));

            button_entity = new Button("Spawn Entities");
            button_entity.LocalPosition = new Vector2i(5, 5);
            Container.AddControl(button_entity);
            button_entity.Clicked += button_entity_Clicked;

            button_tile = new Button("Spawn Tiles");
            button_tile.LocalPosition = new Vector2i(0, 5);
            button_tile.Alignment = Align.Bottom;
            button_entity.AddControl(button_tile);
            button_tile.Clicked += button_tile_Clicked;

            button_quit = new Button("Quit");
            button_quit.LocalPosition = new Vector2i(0, 20);
            button_quit.Alignment = Align.Bottom;
            button_tile.AddControl(button_quit);
            button_quit.Clicked += button_quit_Clicked;
        }

        private void button_quit_Clicked(Button sender)
        {
            _netMgr.ClientDisconnect("Client disconnected from game.");
            IoCManager.Resolve<IStateManager>().RequestStateChange<MainScreen>();
            Dispose();
        }

        private void button_tile_Clicked(Button sender)
        {
            _userInterfaceManager.DisposeAllComponents<TileSpawnPanel>(); //Remove old ones.
            _userInterfaceManager.AddComponent(new TileSpawnPanel(new Vector2i(350, 410)));
            //Create a new one.
            ToggleVisible();
            _userInterfaceManager.RemoveFocus(this);
        }

        private void button_entity_Clicked(Button sender)
        {
            _userInterfaceManager.DisposeAllComponents<EntitySpawnPanel>(); //Remove old ones.
            _userInterfaceManager.AddComponent(new EntitySpawnPanel(new Vector2i(350, 410)));
            //Create a new one.
            ToggleVisible();
            _userInterfaceManager.RemoveFocus(this);
        }

        override protected void CloseButtonClicked(ImageButton sender)
        {
            ToggleVisible();
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (e.Key == Keyboard.Key.Escape)
            {
                if (IsVisible())
                {
                    ToggleVisible();
                    return true;
                }
            }
            return false;
        }
    }
}
