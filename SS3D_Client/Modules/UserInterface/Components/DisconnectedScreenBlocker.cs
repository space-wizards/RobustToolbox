using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS13.Modules.Network;
using Lidgren.Network;
using SS13_Shared;
using SS13.States;
using SS13.Modules;
using SS13.UserInterface;

namespace SS13.UserInterface
{
    public class DisconnectedScreenBlocker : GuiComponent
    {
        Label message;
        Button mainMenuButton;

        StateManager stateMgr;

        public DisconnectedScreenBlocker(StateManager _stateMgr, string msg = "Connection closed.")
            :base()
        {
            stateMgr = _stateMgr;
            UiManager.Singleton.DisposeAllComponents();
            message = new Label(msg);
            mainMenuButton = new Button("Main Menu");
            mainMenuButton.Clicked += new Button.ButtonPressHandler(mainMenuButton_Clicked);
            mainMenuButton.label.Color = Color.WhiteSmoke;
            message.Text.Color = Color.WhiteSmoke;
        }

        void mainMenuButton_Clicked(Button sender)
        {
            stateMgr.RequestStateChange(typeof(ConnectMenu));
        }

        public override void Update()
        {
            message.Position = new Point((int)(Gorgon.Screen.Width / 2f - message.ClientArea.Width / 2f), (int)(Gorgon.Screen.Height / 2f - message.ClientArea.Height / 2f) - 50);
            message.Update();
            mainMenuButton.Position = new Point((int)(Gorgon.Screen.Width / 2f - message.ClientArea.Width / 2f), message.ClientArea.Bottom + 20);
            mainMenuButton.Update();
        }

        public override void Render()
        {
            Gorgon.Screen.FilledRectangle(0,0,Gorgon.Screen.Width, Gorgon.Screen.Height, Color.Black);
            message.Render();
            mainMenuButton.Render();
        }

        public override void Dispose()
        {
            message.Dispose();
            mainMenuButton.Dispose();
            base.Dispose();
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            mainMenuButton.MouseDown(e);
            return true;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            mainMenuButton.MouseUp(e);
            return true;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            mainMenuButton.MouseMove(e);
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            return true;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            mainMenuButton.KeyDown(e);
            return true;
        }
    }
}
