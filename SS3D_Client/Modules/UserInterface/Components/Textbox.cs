using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ClientServices.Resources;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using GorgonLibrary.GUI;
using SS13.UserInterface;
using Lidgren.Network;
using SS13_Shared;

namespace SS13.UserInterface
{
    class Textbox : GuiComponent
    {
        Sprite TextboxMain;
        Sprite TextboxLeft;
        Sprite TextboxRight;

        public TextSprite label;

        public delegate void TextSubmitHandler(string text);
        public event TextSubmitHandler OnSubmit;

        private Rectangle clientAreaMain;
        private Rectangle clientAreaLeft;
        private Rectangle clientAreaRight;

        public string Text = "";
        public bool ClearOnSubmit = true;
        public bool ClearFocusOnSubmit = true;
        public int maxCharacters = 20;
        public int Width = 0;

        public Textbox(int width)
            : base()
        {
            TextboxLeft = ResourceManager.GetSprite("button_left");
            TextboxMain = ResourceManager.GetSprite("button_middle");
            TextboxRight = ResourceManager.GetSprite("button_right");

            Width = width;

            label = new TextSprite("Textbox", "", ResourceManager.GetFont("CALIBRI"));
            label.Color = System.Drawing.Color.Black;

            Update();
        }

        public override void Update()
        {

            clientAreaLeft = new Rectangle(this.position, new Size((int)TextboxLeft.Width, (int)TextboxLeft.Height));
            clientAreaMain = new Rectangle(new Point(clientAreaLeft.Right, this.position.Y), new Size(Width, (int)TextboxMain.Height));
            clientAreaRight = new Rectangle(new Point(clientAreaMain.Right, this.position.Y), new Size((int)TextboxRight.Width, (int)TextboxRight.Height));
            clientArea = new Rectangle(this.position, new Size(clientAreaLeft.Width + clientAreaMain.Width + clientAreaRight.Width, clientAreaMain.Height));
            label.Position = new Point(clientAreaLeft.Right, this.position.Y + (int)(clientArea.Height / 2f) - (int)(label.Height / 2f));

            if (Focus) label.Text = Text + "|";
            else label.Text = Text;
        }

        public override void Render()
        {
            TextboxLeft.Draw(clientAreaLeft);
            TextboxMain.Draw(clientAreaMain);
            TextboxRight.Draw(clientAreaRight);
            label.Draw();
        }

        public override void Dispose()
        {
            label = null;
            TextboxLeft = null;
            TextboxMain = null;
            TextboxRight = null;
            OnSubmit = null;
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (clientArea.Contains(new Point((int)e.Position.X, (int)e.Position.Y))) //Needed so it grabs focus when clicked.
                return true;

            return false;
        }

        public override bool MouseUp(MouseInputEventArgs e)
        {
            return false;
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (!Focus) return false;
            if (e.Key == KeyboardKeys.Return && Text.Length >= 1)
            {
                Submit();
                return true;
            }
            if (e.Key == KeyboardKeys.Back && Text.Length >= 1)
            {
                Text = Text.Substring(0, Text.Length - 1);
                return true;
            }
            else if (char.IsLetterOrDigit(e.CharacterMapping.Character))
            {
                if (Text.Length == maxCharacters) return false;
                if (e.Shift)
                {
                    Text += e.CharacterMapping.Shifted;
                }
                else
                {
                    Text += e.CharacterMapping.Character;
                }
                return true;
            }
            return false;
        }

        private void Submit()
        {
            if (OnSubmit != null) OnSubmit(Text);
            if (ClearOnSubmit) Text = string.Empty;
            if (ClearFocusOnSubmit) Focus = false;
        }
    }
}
