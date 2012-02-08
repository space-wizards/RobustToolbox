using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using ClientInterfaces;
using ClientInterfaces.Input;
using ClientInterfaces.UserInterface;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS13_Shared;

namespace ClientServices.UserInterface.Components
{
    public class Chatbox : GuiComponent
    {
        private readonly IResourceManager _resourceManager;
        private readonly IUserInterfaceManager _userInterfaceManager;
        private readonly IKeyBindingManager _keyBindingManager;

        public delegate void TextSubmitHandler(Chatbox Chatbox, string Text);
        public event TextSubmitHandler TextSubmitted;

        private readonly List<Label> _entries = new List<Label>();

        private Label _textInputLabel;
        private Sprite _backgroundSprite;
        private RenderImage _renderImage;

        private const int maxLines = 20;
        private int _chatMessages;
        private const int MaxLineLength = 90;
        private readonly Dictionary<ChatChannel, Color> _chatColors;

        private bool _disposing;
        private bool _active;

        public bool Active
        {
            get { return _active; }
            set
            {
                _active = value;
                //HACK
                _keyBindingManager.Enabled = !_active;
            }
        }

        public Chatbox(IResourceManager resourceManager, IUserInterfaceManager userInterfaceManager)
        {
            _resourceManager = resourceManager;
            _userInterfaceManager = userInterfaceManager;
            
            ClientArea = new Rectangle(5, Gorgon.Screen.Height - 205, 600, 200); //!!! Use this instead of Window Dimensions
            Position = new Point(5, Gorgon.Screen.Height - 205);

            _backgroundSprite = _resourceManager.GetSprite("1pxwhite");

            _textInputLabel = new Label("", _resourceManager);
            _textInputLabel.Text.Size = new Size(ClientArea.Width - 10, 12);
            _textInputLabel.Position = new Point(Position.X + 2, Position.Y + ClientArea.Size.Height - 10);
            _textInputLabel.Text.Color = Color.Green;
            _textInputLabel.Text.WordWrap = true;

            _chatColors = new Dictionary<ChatChannel, Color>();
            _chatColors.Add(ChatChannel.Default, Color.Gray);
            _chatColors.Add(ChatChannel.Damage, Color.Red);
            _chatColors.Add(ChatChannel.Radio, Color.DarkGreen);
            _chatColors.Add(ChatChannel.Server, Color.Blue);
            _chatColors.Add(ChatChannel.Player, Color.Green);
            _chatColors.Add(ChatChannel.Lobby, Color.White);
            _chatColors.Add(ChatChannel.Ingame, Color.Green);

            _renderImage = new RenderImage("chatboxRI", ClientArea.Size.Width, ClientArea.Size.Height,
                                          ImageBufferFormats.BufferUnknown) {ClearEachFrame = ClearTargets.None};

            PreRender();
        }

        private void PreRender()
        {
            var renderPos = new Point(0, 0);

            _renderImage.BeginDrawing();
            _backgroundSprite.Color = System.Drawing.Color.FromArgb(51, 56, 64);
            _backgroundSprite.Opacity = 240;
            _backgroundSprite.Draw(new Rectangle(renderPos, new Size(ClientArea.Width, ClientArea.Height)));

            Sprite corner_top_left = _resourceManager.GetSprite("corner_top_left");
            corner_top_left.Draw(new Rectangle(renderPos.X, renderPos.Y, (int)corner_top_left.Width, (int)corner_top_left.Height));

            Sprite corner_top_right = _resourceManager.GetSprite("corner_top_right");
            corner_top_right.Draw(new Rectangle(renderPos.X + ClientArea.Width - (int)corner_top_right.Width, renderPos.Y, (int)corner_top_right.Width, (int)corner_top_right.Height));

            Sprite border_top = _resourceManager.GetSprite("border_top");
            border_top.Draw(new Rectangle(renderPos.X + (int)corner_top_left.Width, renderPos.Y, ClientArea.Width - (int)corner_top_left.Width - (int)corner_top_right.Width, (int)border_top.Height));

            Sprite corner_bottom_left = _resourceManager.GetSprite("corner_bottom_left");
            corner_bottom_left.Draw(new Rectangle(renderPos.X, renderPos.Y + ClientArea.Height - (int)corner_bottom_left.Height, (int)corner_bottom_left.Width, (int)corner_bottom_left.Height));

            Sprite corner_bottom_right = _resourceManager.GetSprite("corner_bottom_right");
            corner_bottom_right.Draw(new Rectangle(renderPos.X + ClientArea.Width - (int)corner_bottom_right.Width, renderPos.Y + ClientArea.Height - (int)corner_bottom_right.Height, (int)corner_bottom_right.Width, (int)corner_bottom_right.Height));

            Sprite border_left = _resourceManager.GetSprite("border_left");
            border_left.Draw(new Rectangle(renderPos.X, renderPos.Y + (int)corner_top_left.Height, (int)border_left.Width, ClientArea.Height - (int)corner_bottom_left.Height - (int)corner_top_left.Height));

            Sprite border_right = _resourceManager.GetSprite("border_right");
            border_right.Draw(new Rectangle(renderPos.X + ClientArea.Width - (int)border_right.Width, renderPos.Y + (int)corner_top_right.Height, (int)border_right.Width, ClientArea.Height - (int)corner_bottom_right.Height - (int)corner_top_right.Height));

            Sprite border_bottom = _resourceManager.GetSprite("border_bottom");
            border_bottom.Draw(new Rectangle(renderPos.X + (int)corner_top_left.Width, renderPos.Y + ClientArea.Height - (int)border_bottom.Height, ClientArea.Width - (int)corner_bottom_left.Width - (int)corner_bottom_right.Width, (int)border_bottom.Height));

            Sprite corner_middle_left = _resourceManager.GetSprite("corner_middle_left");
            corner_middle_left.Draw(new Rectangle(renderPos.X, renderPos.Y + ClientArea.Height - 16 - (int)corner_middle_left.Height, (int)corner_middle_left.Width, (int)corner_middle_left.Height));

            Sprite corner_middle_right = _resourceManager.GetSprite("corner_middle_right");
            corner_middle_right.Draw(new Rectangle(renderPos.X + ClientArea.Width - (int)corner_middle_right.Width, renderPos.Y + ClientArea.Height - 16 - (int)corner_middle_right.Height, (int)corner_middle_right.Width, (int)corner_middle_right.Height));

            Sprite border_middle = _resourceManager.GetSprite("border_middle_h");
            border_middle.Draw(new Rectangle(renderPos.X + (int)corner_middle_left.Width, renderPos.Y + ClientArea.Height - 16 - (int)border_middle.Height, ClientArea.Width - (int)corner_middle_left.Width - (int)corner_middle_right.Width, (int)border_middle.Height));

            _renderImage.EndDrawing();
        }

        public void AddLine(string message, ChatChannel channel)
        {
            if (_disposing) return;

            var charCount = 0;
            var messageSplit = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .GroupBy(w => (charCount += w.Length + 1) / MaxLineLength)
                            .Select(g => string.Join(" ", g));

            foreach (string part in messageSplit)
            {
                var label = new Label(part, _resourceManager);
                label.Text.Size = new Size(ClientArea.Width - 10, 12);
                label.Text.Color = _chatColors[channel];
                _entries.Add(label);
                _chatMessages++;
            }

            DrawLines();
        }

        private void DrawLines()
        {
            _textInputLabel.Position = new Point(this.ClientArea.X + 4, ClientArea.Y + ClientArea.Height - 20);
            _textInputLabel.Render();

            while (_entries.Count > maxLines)
                _entries.RemoveAt(0);

            int start = Math.Max(0, _entries.Count - 12);

            for (int i = _entries.Count - 1; i >= start; i--)
            {
                _entries[i].Position = new Point(this.ClientArea.X + 2, this.ClientArea.Y + this.ClientArea.Height - (14 * (_entries.Count - i)) - 26);
                _entries[i].Render();
            }
        }

        public override bool KeyDown(KeyboardInputEventArgs e)
        {
            if (e.Key == KeyboardKeys.T && !Active)
            {
                _userInterfaceManager.SetFocus(this);
                Active = true;
                return true;
            }

            if (!Active)
                return false;

            if (e.Key == KeyboardKeys.Enter)
            {
                if (TextSubmitted != null) TextSubmitted(this, _textInputLabel.Text.Text);
                _textInputLabel.Text.Text = "";
                Active = false;
                return true;
            }

            if (e.Key == KeyboardKeys.Back)
            {
                if (_textInputLabel.Text.Text.Length > 0)
                    _textInputLabel.Text.Text = _textInputLabel.Text.Text.Remove(_textInputLabel.Text.Text.Length - 1, 1);
                return true;
            }

            if (char.IsLetterOrDigit(e.CharacterMapping.Character) || char.IsPunctuation(e.CharacterMapping.Character) || char.IsWhiteSpace(e.CharacterMapping.Character))
            {
                if (e.Shift)
                    _textInputLabel.Text.Text += e.CharacterMapping.Shifted;
                else
                    _textInputLabel.Text.Text += e.CharacterMapping.Character;
            }
            return true;
        }

        public override void Dispose()
        {
            _disposing = true;
            TextSubmitted = null;
            _entries.Clear();
            _textInputLabel = null;
            _backgroundSprite = null;
            if (_renderImage != null && _renderImage.Image != null) _renderImage.Dispose();
            _renderImage = null;
            _chatColors.Clear();
            base.Dispose();
        }

        public override void Update()
        {
            base.Update();
            _textInputLabel.Update();
            foreach (var l in _entries) l.Update();
        }

        public override void Render()
        {
            if (_disposing) return;

            if (IsVisible())
            {
                _renderImage.Blit(ClientArea.X, ClientArea.Y);
                DrawLines();
            }
        }
    }
}