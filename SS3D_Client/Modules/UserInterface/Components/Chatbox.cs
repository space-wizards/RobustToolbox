using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using GorgonLibrary;
using GorgonLibrary.Framework;
using GorgonLibrary.GUI;
using GorgonLibrary.Graphics;
using GorgonLibrary.Graphics.Utilities;
using GorgonLibrary.InputDevices;
using ClientResourceManager;

namespace SS13.UserInterface
{
    public class Chatbox : GuiComponent
    {
        public delegate void TextSubmitHandler(Chatbox Chatbox, string Text);
        public event TextSubmitHandler TextSubmitted;

        private List<Label> entries = new List<Label>();

        private Label textInputLabel;
        private Sprite backgroundSprite;
        private RenderImage renderImage;

        private readonly int maxLines = 20;
        private int chatMessages = 0;
        private int maxLineLength = 90;
        private Dictionary<ChatChannel, System.Drawing.Color> chatColors;

        private bool disposing = false;

        private bool active = false;

        private GorgonLibrary.Graphics.Font font;

        public bool Active
        {
            get { return active; }
            set
            {
                active = value;
                //HACK
                ClientServices.Input.KeyBindingManager.Singleton.Enabled = !active;
            }
        }

        public Chatbox(string name)
            : base()
        {
            ClientArea = new Rectangle(5, Gorgon.Screen.Height - 205, 600, 200); //!!! Use this instead of Window Dimensions
            Position = new Point(5, Gorgon.Screen.Height - 205);

            font = ResMgr.Singleton.GetFont("CALIBRI");

            backgroundSprite = ResMgr.Singleton.GetSprite("1pxwhite");

            textInputLabel = new Label("");
            textInputLabel.Text.Size = new System.Drawing.Size(ClientArea.Width - 10, 12);
            textInputLabel.Position = new System.Drawing.Point(this.Position.X + 2, this.Position.Y + this.ClientArea.Size.Height - 10);
            textInputLabel.Text.Color = System.Drawing.Color.Green;
            textInputLabel.Text.WordWrap = true;

            chatColors = new Dictionary<ChatChannel, Color>();
            chatColors.Add(ChatChannel.Default, System.Drawing.Color.Gray);
            chatColors.Add(ChatChannel.Damage, System.Drawing.Color.Red);
            chatColors.Add(ChatChannel.Radio, System.Drawing.Color.DarkGreen);
            chatColors.Add(ChatChannel.Server, System.Drawing.Color.Blue);
            chatColors.Add(ChatChannel.Player, System.Drawing.Color.Green);
            chatColors.Add(ChatChannel.Lobby, System.Drawing.Color.White);
            chatColors.Add(ChatChannel.Ingame, System.Drawing.Color.Green);

            renderImage = new RenderImage("chatboxRI", this.ClientArea.Size.Width, this.ClientArea.Size.Height, ImageBufferFormats.BufferUnknown);
            renderImage.ClearEachFrame = ClearTargets.None;

            PreRender();
        }

        private void PreRender()
        {
            Point renderPos = new Point(0, 0);

            renderImage.BeginDrawing();
            backgroundSprite.Color = System.Drawing.Color.FromArgb(51, 56, 64);
            backgroundSprite.Opacity = 240;
            backgroundSprite.Draw(new Rectangle(renderPos, new Size(clientArea.Width, clientArea.Height)));

            Sprite corner_top_left = ResMgr.Singleton.GetSprite("corner_top_left");
            corner_top_left.Draw(new Rectangle(renderPos.X, renderPos.Y, (int)corner_top_left.Width, (int)corner_top_left.Height));

            Sprite corner_top_right = ResMgr.Singleton.GetSprite("corner_top_right");
            corner_top_right.Draw(new Rectangle(renderPos.X + ClientArea.Width - (int)corner_top_right.Width, renderPos.Y, (int)corner_top_right.Width, (int)corner_top_right.Height));

            Sprite border_top = ResMgr.Singleton.GetSprite("border_top");
            border_top.Draw(new Rectangle(renderPos.X + (int)corner_top_left.Width, renderPos.Y, ClientArea.Width - (int)corner_top_left.Width - (int)corner_top_right.Width, (int)border_top.Height));

            Sprite corner_bottom_left = ResMgr.Singleton.GetSprite("corner_bottom_left");
            corner_bottom_left.Draw(new Rectangle(renderPos.X, renderPos.Y + ClientArea.Height - (int)corner_bottom_left.Height, (int)corner_bottom_left.Width, (int)corner_bottom_left.Height));

            Sprite corner_bottom_right = ResMgr.Singleton.GetSprite("corner_bottom_right");
            corner_bottom_right.Draw(new Rectangle(renderPos.X + ClientArea.Width - (int)corner_bottom_right.Width, renderPos.Y + ClientArea.Height - (int)corner_bottom_right.Height, (int)corner_bottom_right.Width, (int)corner_bottom_right.Height));

            Sprite border_left = ResMgr.Singleton.GetSprite("border_left");
            border_left.Draw(new Rectangle(renderPos.X, renderPos.Y + (int)corner_top_left.Height, (int)border_left.Width, ClientArea.Height - (int)corner_bottom_left.Height - (int)corner_top_left.Height));

            Sprite border_right = ResMgr.Singleton.GetSprite("border_right");
            border_right.Draw(new Rectangle(renderPos.X + ClientArea.Width - (int)border_right.Width, renderPos.Y + (int)corner_top_right.Height, (int)border_right.Width, ClientArea.Height - (int)corner_bottom_right.Height - (int)corner_top_right.Height));

            Sprite border_bottom = ResMgr.Singleton.GetSprite("border_bottom");
            border_bottom.Draw(new Rectangle(renderPos.X + (int)corner_top_left.Width, renderPos.Y + ClientArea.Height - (int)border_bottom.Height, ClientArea.Width - (int)corner_bottom_left.Width - (int)corner_bottom_right.Width, (int)border_bottom.Height));

            Sprite corner_middle_left = ResMgr.Singleton.GetSprite("corner_middle_left");
            corner_middle_left.Draw(new Rectangle(renderPos.X, renderPos.Y + ClientArea.Height - 16 - (int)corner_middle_left.Height, (int)corner_middle_left.Width, (int)corner_middle_left.Height));

            Sprite corner_middle_right = ResMgr.Singleton.GetSprite("corner_middle_right");
            corner_middle_right.Draw(new Rectangle(renderPos.X + ClientArea.Width - (int)corner_middle_right.Width, renderPos.Y + ClientArea.Height - 16 - (int)corner_middle_right.Height, (int)corner_middle_right.Width, (int)corner_middle_right.Height));

            Sprite border_middle = ResMgr.Singleton.GetSprite("border_middle_h");
            border_middle.Draw(new Rectangle(renderPos.X + (int)corner_middle_left.Width, renderPos.Y + ClientArea.Height - 16 - (int)border_middle.Height, ClientArea.Width - (int)corner_middle_left.Width - (int)corner_middle_right.Width, (int)border_middle.Height));

            renderImage.EndDrawing();
        }

        public void AddLine(string message, ChatChannel channel)
        {
            if (disposing) return;
            int charCount = 0;
            IEnumerable<string> messageSplit = message.Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .GroupBy(w => (charCount += w.Length + 1) / maxLineLength)
                            .Select(g => string.Join(" ", g));

            foreach (string str in messageSplit)
            {
                Label label = new Label(str);
                label.Text.Size = new System.Drawing.Size(ClientArea.Width - 10, 12);
                label.Text.Color = chatColors[channel];
                entries.Add(label);
                chatMessages++;
            }

            drawLines();
        }

        private void drawLines()
        {
            textInputLabel.Position = new System.Drawing.Point(this.ClientArea.X + 4, ClientArea.Y + ClientArea.Height - 20);
            textInputLabel.Render();

            while (entries.Count > maxLines)
                entries.RemoveAt(0);

            int start = Math.Max(0, entries.Count - 12);

            for (int i = entries.Count - 1; i >= start; i--)
            {
                entries[i].Position = new System.Drawing.Point(this.ClientArea.X + 2, this.ClientArea.Y + this.ClientArea.Height - (14 * (entries.Count - i)) - 26);
                entries[i].Render();
            }
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (e.Key == KeyboardKeys.T && !Active)
            {
                UiManager.Singleton.SetFocus(this);
                Active = true;
                return true;
            }

            if (!Active)
                return false;

            if (e.Key == KeyboardKeys.Enter)
            {
                if (TextSubmitted != null) TextSubmitted(this, textInputLabel.Text.Text);
                textInputLabel.Text.Text = "";
                Active = false;
                return true;
            }

            if (e.Key == KeyboardKeys.Back)
            {
                if (textInputLabel.Text.Text.Length > 0)
                    textInputLabel.Text.Text = textInputLabel.Text.Text.Remove(textInputLabel.Text.Text.Length - 1, 1);
                return true;
            }

            if (char.IsLetterOrDigit(e.CharacterMapping.Character) || char.IsPunctuation(e.CharacterMapping.Character) || char.IsWhiteSpace(e.CharacterMapping.Character))
            {
                if (e.Shift)
                    textInputLabel.Text.Text += e.CharacterMapping.Shifted;
                else
                    textInputLabel.Text.Text += e.CharacterMapping.Character;
            }
            return true;
        }

        public override void Dispose()
        {
            this.disposing = true;
            TextSubmitted = null;
            entries.Clear();
            textInputLabel = null;
            backgroundSprite = null;
            if (renderImage != null) if (renderImage.Image != null) renderImage.Dispose(); //Fuck this statement.
            renderImage = null;
            chatColors.Clear();
            font = null;
            base.Dispose();
        }

        public override void Update()
        {
            base.Update();
            textInputLabel.Update();
            foreach (Label l in entries) l.Update();
        }

        public override void Render()
        {
            if (disposing) return;

            if (this.IsVisible())
            {
                renderImage.Blit(ClientArea.X, ClientArea.Y);
                drawLines();
            }
        }
    }
}