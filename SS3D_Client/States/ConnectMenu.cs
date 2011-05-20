using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Mogre;

using SS3D.Modules;

using Miyagi;
using Miyagi.UI;
using Miyagi.UI.Controls;
using Miyagi.Common;
using Miyagi.Common.Data;
using Miyagi.Common.Resources;
using Miyagi.Common.Events;
using Miyagi.TwoD;


namespace SS3D.States
{
  public class ConnectMenu : State
  {

    private OgreManager mEngine;
    private StateManager mStateMgr;
    private GUI guiConnectMenu;
    private string ipTextboxIP = "127.0.0.1";
    private string name = "Player";
    private bool connecting = false;
    private DateTime connectTime;
    private float connectTimeOut = 5000f;

    public ConnectMenu()
    {
      mEngine = null;
    }

    #region Startup, Shutdown, Update
    public override bool Startup(StateManager _mgr)
    {
        mEngine = _mgr.Engine;
        mStateMgr = _mgr;

        mEngine.mNetworkMgr.Disconnect();
        mEngine.mNetworkMgr.Connected += new Modules.Network.NetworkStateHandler(mNetworkMgr_Connected);

        // Lets make sure the background is visible
        if (mEngine.mMiyagiSystem.GUIManager.GetGUI("guiBackground") != null)
        {
            mEngine.mMiyagiSystem.GUIManager.GetGUI("guiBackground").Visible = true;
        }
        // If we've been here before, lets just use that menu and not recreated it
        if (mEngine.mMiyagiSystem.GUIManager.GetGUI("guiConnectMenu") == null)
        {
            guiConnectMenu = new GUI("guiConnectMenu");
            guiConnectMenu.ZOrder = 10;
            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(guiConnectMenu);
            CreateMenu();
            guiConnectMenu.Resize(mEngine.ScalarX, mEngine.ScalarY);
        }
        else
        {
            guiConnectMenu = mEngine.mMiyagiSystem.GUIManager.GetGUI("guiConnectMenu");
            guiConnectMenu.Fade(0, 1, 100);
        }

        return true;
    }

    private void CreateMenu()
    {
        Button joinButton = new Button("connectJoinButton")
        {
            Location = new Point(650, 280),
            Size = new Size(160, 40),
            Skin = MiyagiResources.Singleton.Skins["ButtonSkinGreen"],
            TextStyle =
            {
                Alignment = Alignment.MiddleCenter,
                ForegroundColour = Colours.DarkBlue
            }
        };
        joinButton.MouseDown += JoinButtonMouseDown;
        joinButton.Text = "Connect";
        guiConnectMenu.Controls.Add(joinButton);

        Button returnButton = new Button("connectReturnButton")
        {
            Location = new Point(650, 330),
            Size = new Size(160, 40),
            Skin = MiyagiResources.Singleton.Skins["ButtonSkinGreen"],
            TextStyle =
            {
                Alignment = Alignment.MiddleCenter,
                ForegroundColour = Colours.DarkBlue
            }
        };
        returnButton.MouseDown += ReturnButtonMouseDown;
        returnButton.Text = "Return";
        guiConnectMenu.Controls.Add(returnButton);

        TextBox ipTextbox = new TextBox("connectIpTextbox")
        {
            Size = new Size(160, 32),
            Location = new Point(820, 282),
            Padding = new Thickness(2, 2, 2, 2),
            Text = ipTextboxIP,
            TextStyle =
            {
                Alignment = Alignment.MiddleCenter,
            },
            TextBoxStyle =
            {
                CaretStyle =
                {
                    Size = new Size(2, 16),
                    Colour = Colours.Black
                }
            },
            Skin = MiyagiResources.Singleton.Skins["TextBoxSkin"]
        };
        ipTextbox.TextChanged += ipTextBoxChanged;
        guiConnectMenu.Controls.Add(ipTextbox);

        TextBox nameTextbox = new TextBox("nameTextbox")
        {
            Size = new Size(160, 32),
            Location = new Point(820, 330),
            Padding = new Thickness(2, 2, 2, 2),
            Text = name,
            TextStyle =
            {
                Alignment = Alignment.MiddleCenter,
            },
            TextBoxStyle =
            {
                CaretStyle =
                {
                    Size = new Size(2, 16),
                    Colour = Colours.Black
                }
            },
            Skin = MiyagiResources.Singleton.Skins["TextBoxSkin"]
        };
        nameTextbox.TextChanged += new EventHandler<TextEventArgs>(nameTextbox_TextChanged);
        guiConnectMenu.Controls.Add(nameTextbox);

    }

    void nameTextbox_TextChanged(object sender, TextEventArgs e)
    {
        name = ((TextBox)sender).Text;
    }

    private void ipTextBoxChanged(object sender, Miyagi.Common.Events.TextEventArgs e)
    {
        ipTextboxIP = ((TextBox)sender).Text;
    }

    void mNetworkMgr_Connected(Modules.Network.NetworkManager netMgr)
    {
        connecting = false;
        //guiConnectMenu.GetControl("connectJoinButton").Enabled = true;
        //((Button)guiConnectMenu.GetControl("connectJoinButton")).Text = "Join";
        //Send client name
        mEngine.mNetworkMgr.SendClientName(name);
        mStateMgr.RequestStateChange(typeof(GameScreen));
    }

    private void JoinButtonMouseDown(object sender, MouseButtonEventArgs e)
    {
        StartConnect();
    }

    // called when join button pressed and also if autoconnecting at startup
    public void StartConnect()
    {
        connectTime = DateTime.Now;
        connecting = true;
        mEngine.mNetworkMgr.ConnectTo(ipTextboxIP);
        guiConnectMenu.GetControl("connectJoinButton").Enabled = false;
        ((Button)guiConnectMenu.GetControl("connectJoinButton")).Text = "Trying";
    }

    private void ReturnButtonMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (connecting)
        {
            connecting = false;
            guiConnectMenu.GetControl("connectJoinButton").Enabled = true;
            ((Button)guiConnectMenu.GetControl("connectJoinButton")).Text = "Retry";
            mEngine.mNetworkMgr.Disconnect();
        }
        mStateMgr.RequestStateChange(typeof(MainMenu));
    }
 
    public override void Shutdown()
    {
        mEngine.mNetworkMgr.Connected -= new Modules.Network.NetworkStateHandler(mNetworkMgr_Connected);
        guiConnectMenu.Fade(1, 0, 100);
    }

    public override void Update(long _frameTime)
    {
        if (connecting)
        {
            TimeSpan dif = DateTime.Now - connectTime;
            if (dif.TotalMilliseconds > connectTimeOut)
            {
                connecting = false;
                guiConnectMenu.GetControl("connectJoinButton").Enabled = true;
                ((Button)guiConnectMenu.GetControl("connectJoinButton")).Text = "Retry";
                mEngine.mNetworkMgr.Disconnect();
            }
        }
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
