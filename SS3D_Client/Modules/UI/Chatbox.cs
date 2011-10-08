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

namespace SS3D.Modules.UI
{
    public class Chatbox : GUIWindow
    {
        public delegate void TextSubmitHandler(Chatbox Chatbox, string Text);
        private List<TextSprite> entries = new List<TextSprite>();

        private TextSprite textInputLabel;
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
            : base(name, 5, Gorgon.Screen.Height - 205, 600, 200)
        {
            var desktop = UIDesktop.Singleton;

            font = ResMgr.Singleton.GetFont("CALIBRI");
            backgroundSprite = ResMgr.Singleton.GetSprite("1pxwhite");
            backgroundSprite.Color = System.Drawing.Color.FromArgb(51, 56, 64);
            backgroundSprite.Opacity = 240;
            backgroundSprite.Position = Position;
            backgroundSprite.Size = Size;

            HasCaption = false;
            KeyDown += new KeyboardInputEvent(chatGUI_KeyDown);
            MouseDown += new MouseInputEvent(Chatbox_MouseDown);
            BackgroundColor = System.Drawing.Color.DarkGray;
            desktop.Windows.Add(this);
            textInputLabel = new TextSprite("inputlabel", "", font);
            textInputLabel.Size = new System.Drawing.Size(ClientArea.Width - 10, 12);
            textInputLabel.Position = new System.Drawing.Point(this.Position.X + 2, this.Position.Y + this.Size.Height - 10);
            textInputLabel.Color = System.Drawing.Color.Green;
            textInputLabel.WordWrap = true;

            chatColors = new Dictionary<ChatChannel, Color>();
            chatColors.Add(ChatChannel.Default, System.Drawing.Color.Gray);
            chatColors.Add(ChatChannel.Damage, System.Drawing.Color.Red);
            chatColors.Add(ChatChannel.Radio, System.Drawing.Color.DarkGreen);
            chatColors.Add(ChatChannel.Server, System.Drawing.Color.Blue);
            chatColors.Add(ChatChannel.Player, System.Drawing.Color.Green);
            chatColors.Add(ChatChannel.Lobby, System.Drawing.Color.White);
            chatColors.Add(ChatChannel.Ingame, System.Drawing.Color.Green);

            renderImage = new RenderImage("chatboxRI", WindowDimensions.Width, WindowDimensions.Height, ImageBufferFormats.BufferUnknown);
            renderImage.ClearEachFrame = ClearTargets.None;
            PreRender();
        }

        private void PreRender()
        {
            Point renderPos = new Point(0, 0);

            renderImage.BeginDrawing();
            backgroundSprite.Color = System.Drawing.Color.FromArgb(51, 56, 64);
            backgroundSprite.Opacity = 240;
            backgroundSprite.Position = renderPos;
            backgroundSprite.Size = Size;
            backgroundSprite.Draw();

            if (Skin == null)
                return;

            Skin.Elements["Window.Border.Top.LeftCorner"].Draw(new System.Drawing.Rectangle(renderPos.X, renderPos.Y, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.LeftCorner").Height));
            Skin.Elements["Window.Border.Top.Horizontal"].Draw(new System.Drawing.Rectangle(renderPos.X + ResMgr.Singleton.GetGUIInfo("Window.Border.Top.LeftCorner").Width, renderPos.Y, WindowDimensions.Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.Horizontal").Height));
            Skin.Elements["Window.Border.Top.RightCorner"].Draw(new System.Drawing.Rectangle(renderPos.X + WindowDimensions.Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Width, renderPos.Y, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Top.RightCorner").Height));

            Skin.Elements["Window.Border.Vertical.Left"].Draw(new System.Drawing.Rectangle(renderPos.X, DefaultCaptionHeight + renderPos.Y, DefaultBorderSize, WindowDimensions.Height - DefaultCaptionHeight - DefaultBorderHeight));
            Skin.Elements["Window.Border.Vertical.Right"].Draw(new System.Drawing.Rectangle(renderPos.X + WindowDimensions.Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Vertical.Right").Width, DefaultCaptionHeight + renderPos.Y, DefaultBorderSize, WindowDimensions.Height - DefaultCaptionHeight - DefaultBorderHeight));

            Skin.Elements["Window.Border.Middle.LeftCorner"].Draw(new System.Drawing.Rectangle(renderPos.X, renderPos.Y + WindowDimensions.Height - 16 - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Height, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Height));
            Skin.Elements["Window.Border.Middle.Horizontal"].Draw(new System.Drawing.Rectangle(renderPos.X + ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Width, renderPos.Y + WindowDimensions.Height - 16 - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.Horizontal").Height, WindowDimensions.Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.Horizontal").Height));
            Skin.Elements["Window.Border.Middle.RightCorner"].Draw(new System.Drawing.Rectangle(renderPos.X + WindowDimensions.Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Width, renderPos.Y + WindowDimensions.Height - 16 - ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Height, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.RightCorner").Height));

            Skin.Elements["Window.Border.Bottom.LeftCorner"].Draw(new System.Drawing.Rectangle(renderPos.X, renderPos.Y + WindowDimensions.Height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Height, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Height));
            Skin.Elements["Window.Border.Bottom.Horizontal"].Draw(new System.Drawing.Rectangle(renderPos.X + ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Width, renderPos.Y + WindowDimensions.Height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.Horizontal").Height, WindowDimensions.Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.LeftCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.Horizontal").Height));
            Skin.Elements["Window.Border.Bottom.RightCorner"].Draw(new System.Drawing.Rectangle(renderPos.X + WindowDimensions.Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Width, renderPos.Y + WindowDimensions.Height - ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Height, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Width, ResMgr.Singleton.GetGUIInfo("Window.Border.Bottom.RightCorner").Height));

            Skin.Elements["Window.Border.Scrollbar.Vertical"].Draw(new Rectangle(renderPos.X + WindowDimensions.Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Vertical.Right").Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Scrollbar.Scroll").Width - ResMgr.Singleton.GetGUIInfo("Window.Border.Scrollbar.Vertical").Width, renderPos.Y + ResMgr.Singleton.GetGUIInfo("Window.Border.Top.Horizontal").Height - 1, ResMgr.Singleton.GetGUIInfo("Window.Border.Scrollbar.Vertical").Width, WindowDimensions.Height - 14 - (ResMgr.Singleton.GetGUIInfo("Window.Border.Middle.Horizontal").Height * 2)));

            renderImage.EndDrawing();

        }

        private void Chatbox_MouseDown(object sender, MouseInputEventArgs e)
        {
            throw new NotImplementedException();
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
                TextSprite label = new TextSprite("label" + entries.Count, str, font);
                label.Size = new System.Drawing.Size(ClientArea.Width - 10, 12);
                label.Color = chatColors[channel];
                entries.Add(label);
                chatMessages++;
            }
            drawLines();
        }

        private void drawLines()
        {
            textInputLabel.Position = new System.Drawing.Point(this.Position.X + 4, Position.Y + WindowDimensions.Height - 20);
            textInputLabel.Draw();

            while (entries.Count > maxLines)
                entries.RemoveAt(0);

            int start = Math.Max(0, entries.Count - 12);

            for (int i = entries.Count - 1; i >= start; i--)
            {
                entries[i].Position = new System.Drawing.Point(this.Position.X + 2, this.Position.Y + this.Size.Height - (14 * (entries.Count - i)) - 26);
                entries[i].Draw();
            }
        }

        /// <summary>
        /// Processes keypresses into meaningful input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chatGUI_KeyDown(object sender, KeyboardInputEventArgs e)
        {
            var keyboard = UIDesktop.Singleton.Input.Keyboard;

            if (e.Key == KeyboardKeys.T && !Active)
            {
                Active = true;
                return;
            }

            if (!Active)
                return;
            if (e.Key == KeyboardKeys.Enter)
            {
                if (TextSubmitted != null) TextSubmitted(this, textInputLabel.Text);
                textInputLabel.Text = "";
                Active = false;
                return;
            }

            if (e.Key == KeyboardKeys.Back)
            {
                if (textInputLabel.Text.Length > 0)
                    textInputLabel.Text = textInputLabel.Text.Remove(textInputLabel.Text.Length - 1, 1);
                return;
            }

            if (keyboard.KeyMappings.Contains(e.Key))
            {
                if (keyboard.KeyStates[KeyboardKeys.LShiftKey] == KeyState.Up && keyboard.KeyStates[KeyboardKeys.RShiftKey] == KeyState.Up)
                    textInputLabel.Text += keyboard.KeyMappings[e.Key].Character;
                else
                    textInputLabel.Text += keyboard.KeyMappings[e.Key].Shifted;
            }
        }

        protected override void Dispose(bool disposing)
        {
            this.disposing = true;
            TextSubmitted = null;
            entries.Clear();
            textInputLabel = null;
            backgroundSprite = null;
            if(renderImage != null) if (renderImage.Image != null) renderImage.Dispose(); //Fuck this statement.
            renderImage = null;
            chatColors.Clear();
            font = null;
            base.Dispose(disposing);
        }

        private void destroy()
        {
        }

        protected override void Draw()
        {
            if (disposing) return;
            System.Drawing.Rectangle screenPoints;		// Screen coordinates.

            if (Visible)
            {
                screenPoints = RectToScreen(ClientArea);

                //Gorgon.CurrentRenderTarget.BeginDrawing();

                renderImage.Blit(Position.X, Position.Y);

                drawLines();
                
                //Gorgon.CurrentRenderTarget.EndDrawing();
            }
        }

        private void DrawNonClientArea()
        {
                    
        }

        public event TextSubmitHandler TextSubmitted;
    }

/*    [Obsolete]
    class ChatboxOld
    {
        private MiyagiResources mMiyagiRes;

        public Panel chatPanel
        {
            private set;
            get;
        }

        private TextBox chatTextbox;

        public GUI chatGUI
        {
            private set;
            get;
        }

        public delegate void TextSubmitHandler(Chatbox Chatbox, string Text);

        private List<Label> entries = new List<Label>();

        private int currentYpos = 0;
        private readonly int maxLines = 20;
        private int transparency = 1;

        public int Transparency
        {
            get
            {
                return transparency;
            }

            set
            {
                transparency = Math.Min(Math.Max(value,1), 100);
                chatGUI.Fade((float)transparency * 0.01f, (float)transparency * 0.01f, 1); //Since we don't have direct access to that :(
            }
        }

        public ChatboxOld(string name)
        {
            mMiyagiRes = MiyagiResources.Singleton;
            chatGUI = new GUI(name);

            this.chatPanel = new Panel(name+"Panel")
            {
                TabStop = false,
                TabIndex = 0,
                Size = new Size(400, 150),
                Location = new Point(0, 0),
                MinSize = new Size(100, 100),
                AlwaysOnTop = false,
                ResizeThreshold = new Thickness(3),
                Padding = new Thickness(2, 2, 2, 2),
                BorderStyle =
                {
                    Thickness = new Thickness(3, 3, 3, 3)
                },
                HScrollBarStyle =
                {
                    ShowButtons = false,
                    Extent = 16,
                    BorderStyle =
                    {
                        Thickness = new Thickness(2, 2, 2, 2)
                    },
                    ThumbStyle =
                    {
                        BorderStyle =
                        {
                            Thickness = new Thickness(2, 2, 2, 2)
                        }
                    }
                },
                VScrollBarStyle =
                {
                    ShowButtons = false,
                    Extent = 16,
                    BorderStyle =
                    {
                        Thickness = new Thickness(2, 2, 2, 2)
                    },
                    ThumbStyle =
                    {
                        BorderStyle =
                        {
                            Thickness = new Thickness(2, 2, 2, 2)
                        }
                    }
                },
                TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
                Skin = MiyagiResources.Singleton.Skins["ConsolePanelSkin"]
            };
            chatPanel.SizeChanged += new EventHandler(chatPanel_SizeChanged);
            chatPanel.LocationChanged += new EventHandler<ChangedValueEventArgs<Point>>(chatPanel_LocationChanged);

            this.chatTextbox = new TextBox("ChatTextbox")
            {
                Size = new Size(400, 30),
                Location = new Point(0, 150),
                Padding = new Thickness(5, 3, 5, 3),
                AlwaysOnTop = false,
                DefocusOnSubmit = false,
                BorderStyle =
                {
                    Thickness = new Thickness(3, 3, 3, 3)
                },
                TextStyle =
                {
                    Offset = new Point(0, 3),
                    Alignment = Alignment.MiddleLeft,
                    ForegroundColour = Colours.White
                },
                TextBoxStyle =
                {
                    CaretStyle =
                    {
                        Size = new Size(2, 16),
                        Colour = Colours.White
                    }
                },
                Skin = MiyagiResources.Singleton.Skins["ConsoleTextBoxSkin"],
                TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
                ClearTextOnSubmit = true
            };

            chatTextbox.Submit += new EventHandler<ValueEventArgs<string>>(chatTextbox_Submit);

            chatGUI.Controls.Add(chatPanel);
            chatGUI.Controls.Add(chatTextbox);

            chatGUI.ZOrder = 10;
        }


        public void SetInputFocus(bool focus = true)
        {
            chatTextbox.Focused = focus;
        }

        public bool HasFocus()
        {
            return chatTextbox.Focused;
        }

        public event TextSubmitHandler TextSubmitted;

        private void chatTextbox_Submit(object sender, ValueEventArgs<string> e)
        {
            if (string.IsNullOrWhiteSpace(e.Data)) return;
            TextSubmitted(this, e.Data);
            //chatTextbox.Focused = false;
        }

        public void AddLine(string text)
        {
            var label = new Label
            {
                Location = new Point(0, currentYpos),
                Text = text,
                AutoSize = true,
                TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
                TextStyle =
                {
                    ForegroundColour = Colours.LightGrey
                }
            };
            label.SuccessfulHitTest += (s, e) => e.Cancel = true;
            this.chatPanel.Controls.Add(label);

            #region Workaround for a bug
            if (!chatGUI.Visible)
            {
                chatGUI.Visible = true;
                this.currentYpos += label.Size.Height;
                chatGUI.Visible = false;
            }
            else this.currentYpos += label.Size.Height; 
            #endregion

            this.entries.Add(label);
            if (entries.Count > maxLines) Trim();
            chatPanel.ScrollToBottom();
        }

        public void AddLine(string text, Colour colour)
        {
            var label = new Label
            {
                Location = new Point(0, currentYpos),
                Text = text,
                AutoSize = true,
                TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
                TextStyle =
                {
                    ForegroundColour = colour
                }
            };
            label.SuccessfulHitTest += (s, e) => e.Cancel = true;
            this.chatPanel.Controls.Add(label);

            #region Workaround for a bug
            if (!chatGUI.Visible)
            {
                chatGUI.Visible = true;
                this.currentYpos += label.Size.Height;
                chatGUI.Visible = false;
            }
            else this.currentYpos += label.Size.Height; 
            #endregion

            this.entries.Add(label);
            if (entries.Count > maxLines) Trim();
            chatPanel.ScrollToBottom();
        }

        void Trim()
        {
            if (entries.Count < 2) return;
            entries[1].Location = new Point(0, 0);
            Label toDelete = entries[0];
            entries.RemoveAt(0);
            currentYpos -= toDelete.Size.Height;
            toDelete.Dispose();
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].Location = (i != 0) ? new Point(0, entries[i - 1].Location.Y + entries[i - 1].Size.Height) : new Point(0, 0);
            }
        }

        void Clear()
        {
            foreach (Label lbl in entries)
            {
                lbl.Dispose();
            }
            currentYpos = 0;
            chatPanel.ScrollToTop();
        }

        void chatPanel_LocationChanged(object sender, ChangedValueEventArgs<Point> e)
        {
            if (chatTextbox != null)
            {
                Point newLoc = new Point(chatPanel.Location.X, chatPanel.Location.Y + chatPanel.Size.Height);
                chatTextbox.Location = newLoc;
            }
        }

        void chatPanel_SizeChanged(object sender, EventArgs e)
        {
            if (chatTextbox != null)
            {
                Size newSize = new Size(chatPanel.Size.Width, 30);
                Point newLoc = new Point(chatPanel.Location.X, chatPanel.Location.Y + chatPanel.Size.Height);
                chatTextbox.Size = newSize;
                chatTextbox.Location = newLoc;
            }
        }
    }//*/
}
