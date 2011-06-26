using System.Collections.Generic;
using System.IO;
using System.Linq;
using Miyagi;
using Miyagi.UI;
using Miyagi.UI.Controls;
using Miyagi.Common;
using Miyagi.Common.Data;
using Miyagi.Common.Resources;
using Miyagi.Common.Events;
using Miyagi.TwoD;
using Mogre;

using SS3D.Modules;

namespace SS3D.States
{
  public class MainMenu : State
  {
    private StateManager mStateMgr;
    private GUI guiMainMenu;
    private GUI guiBackground;
    private Label infoLabel;


    public MainMenu()
    {
      mEngine = null;
    }

    #region Startup, Shutdown, Update
    public override bool Startup(StateManager _mgr)
    {
        mEngine = _mgr.Engine;
        mStateMgr = _mgr;

        // If the menus haven't been generated before, lets do that now, if they have, lets just use them.
        if (mEngine.mMiyagiSystem.GUIManager.GetGUI("guiMainMenu") == null || mEngine.mMiyagiSystem.GUIManager.GetGUI("guiBackground") == null)
        {
            CreateMenu();
            guiMainMenu.Fade(0, 1, 200);
            guiBackground.Fade(0, 1, 200);
            guiMainMenu.Visible = true;
            guiBackground.Visible = true;
        }
        else
        {
            guiMainMenu = mEngine.mMiyagiSystem.GUIManager.GetGUI("guiMainMenu");
            guiBackground = mEngine.mMiyagiSystem.GUIManager.GetGUI("guiBackground");
            guiMainMenu.Fade(0, 1, 100);
            guiMainMenu.Visible = true;
            guiBackground.Visible = true;
        }

        guiMainMenu.EnsureZOrder();
        guiBackground.EnsureZOrder();
        return true;
    }

    public void CreateMenu()
    {
        mEngine.mMiyagiSystem.GUIManager.Cursor = new Cursor(MiyagiResources.Singleton.Skins["CursorSkin"], new Size(16, 16), Point.Empty, true);
        
        guiMainMenu = new GUI("guiMainMenu");
        guiBackground = new GUI("guiBackground");

        Button editButton = new Button("mainEditButton")
        {
            Location = new Point(650, 280),
            Size = new Size(160, 40),
            Skin = MiyagiResources.Singleton.Skins["ButtonSkinGreen"],
            TextStyle =
            {
                Alignment = Alignment.MiddleCenter,
                ForegroundColour = Colours.DarkBlue,
                Font = MiyagiResources.Singleton.Fonts["SpacedockStencil"]
            }
        };
        editButton.MouseDown += EditButtonMouseDown;
        editButton.Text = "Map Edit";
        guiMainMenu.Controls.Add(editButton);

        Button optionsButton = new Button("mainOptionsButton")
        {
            Location = new Point(650, 330),
            Size = new Size(160, 40),
            Skin = MiyagiResources.Singleton.Skins["ButtonSkinGreen"],
            TextStyle =
            {
                Alignment = Alignment.MiddleCenter,
                ForegroundColour = Colours.DarkBlue,
                Font = MiyagiResources.Singleton.Fonts["SpacedockStencil"]
            }
        };
        optionsButton.MouseDown += OptionsButtonMouseDown;
        optionsButton.Text = "Options";
        guiMainMenu.Controls.Add(optionsButton);

        Button connectButton = new Button("mainConnectButton")
        {
            Location = new Point(820, 280),
            Size = new Size(160, 40),
            Skin = MiyagiResources.Singleton.Skins["ButtonSkinGreen"],
            TextStyle =
            {
                Alignment = Alignment.MiddleCenter,
                ForegroundColour = Colours.DarkBlue,
                Font = MiyagiResources.Singleton.Fonts["SpacedockStencil"]
            }
        };

        connectButton.MouseDown += ConnectButtonMouseDown;
        connectButton.Text = "Connect";
        guiMainMenu.Controls.Add(connectButton);

        Button exitButton = new Button("mainExitButton")
        {
            Location = new Point(820, 330),
            Size = new Size(160, 40),
            Skin = MiyagiResources.Singleton.Skins["ButtonSkinGreen"],
            TextStyle =
            {
                Alignment = Alignment.MiddleCenter,
                ForegroundColour = Colours.DarkBlue,
                Font = MiyagiResources.Singleton.Fonts["SpacedockStencil"]
            }
        };
        exitButton.MouseDown += ExitButtonMouseDown;
        exitButton.Text = "Exit";
        guiMainMenu.Controls.Add(exitButton);

        infoLabel = new Label("mainInfoLabel")
        {
            Size = new Size((int)mEngine.Window.Width, 24),
            Location = new Point(24, (int)mEngine.Window.Height - 24),
            Text = "Main menu loaded.",
            TextStyle =
            {
                Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
            }
        }; 
        guiMainMenu.Controls.Add(infoLabel);

        Panel guiBackgroundPanel = new Panel("mainGuiBackgroundPanel")
        {
            Location = new Point(0, 0),
            Size = new Size((int)mEngine.Window.Width, (int)mEngine.Window.Height),
            ResizeMode =  ResizeModes.None,
            Skin = MiyagiResources.Singleton.Skins["MainBackground"],
            AlwaysOnTop = false,
            TextureFiltering = TextureFiltering.Anisotropic
        };
        guiBackground.Controls.Add(guiBackgroundPanel);

        guiMainMenu.ZOrder = 10;
        guiBackground.ZOrder = 5;
        mEngine.mMiyagiSystem.GUIManager.GUIs.Add(guiBackground);
        mEngine.mMiyagiSystem.GUIManager.GUIs.Add(guiMainMenu);

        guiMainMenu.Resize(mEngine.ScalarX, mEngine.ScalarY);

        guiMainMenu.Visible = true;
        guiBackground.Visible = true;
    }


    private void EditButtonMouseDown(object sender, MouseButtonEventArgs e)
    {
        mStateMgr.RequestStateChange(typeof(EditScreen));
    }

    private void ConnectButtonMouseDown(object sender, MouseButtonEventArgs e)
    {
        mStateMgr.RequestStateChange(typeof(ConnectMenu));
    }

    private void OptionsButtonMouseDown(object sender, MouseButtonEventArgs e)
    {
        ((Label)guiMainMenu.GetControl("mainInfoLabel")).Text = "Options pressed";
        mStateMgr.RequestStateChange(typeof(OptionsMenu));
    }

    private void ExitButtonMouseDown(object sender, MouseButtonEventArgs e)
    {
        ((Label)guiMainMenu.GetControl("mainInfoLabel")).Text = "Exit pressed";
        mEngine.Window.Destroy();
        
    }

      public override void Shutdown()
    {
        guiBackground.Visible = false;
        guiMainMenu.Fade(1, 0, 100);
    }

    public override void Update(long _frameTime)
    {
    } 
    #endregion

    #region Input
    public override void UpdateInput(Mogre.FrameEvent evt, MOIS.Keyboard keyState, MOIS.Mouse mouseState)
    {
    }

    public override void KeyDown(MOIS.KeyEvent keyState)
    {
    }

    public override void KeyUp(MOIS.KeyEvent keyState)
    {
    }

    public override void MouseUp(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
    {
    }

    public override void MouseDown(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
    {
    }

    public override void MouseMove(MOIS.MouseEvent mouseState)
    {
    }

    #endregion

  }

}
