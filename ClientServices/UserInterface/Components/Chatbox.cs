using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
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

        public delegate void TextSubmitHandler(Chatbox chatbox, string text);
        public event TextSubmitHandler TextSubmitted;

        private readonly List<Label> _entries = new List<Label>();
        private readonly StringBuilder _currentInputText;

        private Label _textInputLabel;
        private Sprite _backgroundSprite;
        private RenderImage _renderImage;

        private const int MaxLines = 20;
        private const int MaxLineLength = 90;
        private const int MaxLinePixelLength = 585;
        private readonly Dictionary<ChatChannel, Color> _chatColors;

        private bool _disposing;
        private bool _active;

        public bool Active
        {
            get { return _active; }
            set
            {
                _active = value;
                _keyBindingManager.Enabled = !_active;
            }
        }

        public Chatbox(IResourceManager resourceManager, IUserInterfaceManager userInterfaceManager, IKeyBindingManager keyBindingManager)
        {
            _resourceManager = resourceManager;
            _userInterfaceManager = userInterfaceManager;
            _keyBindingManager = keyBindingManager;

            _currentInputText = new StringBuilder();
            
            ClientArea = new Rectangle(5, Gorgon.Screen.Height - 205, 600, 200); //!!! Use this instead of Window Dimensions
            Position = new Point(5, Gorgon.Screen.Height - 205);

            _backgroundSprite = _resourceManager.GetSprite("1pxwhite");

            _textInputLabel = new Label("", _resourceManager)
                                {
                                    Text =
                                        {
                                            Size = new Size(ClientArea.Width - 10, 12),
                                            Color = Color.Green
                                        },
                                    Position = new Point(Position.X + 2, Position.Y + ClientArea.Size.Height - 10),
                                };

            _chatColors = new Dictionary<ChatChannel, Color>
                            {
                                {ChatChannel.Default, Color.Gray},
                                {ChatChannel.Damage, Color.Red},
                                {ChatChannel.Radio, Color.DarkGreen},
                                {ChatChannel.Server, Color.Blue},
                                {ChatChannel.Player, Color.Green},
                                {ChatChannel.Lobby, Color.White},
                                {ChatChannel.Ingame, Color.Green}
                            };

            _renderImage = new RenderImage("chatboxRI", ClientArea.Size.Width, ClientArea.Size.Height,
                                          ImageBufferFormats.BufferUnknown) {ClearEachFrame = ClearTargets.None};

            PreRender();
        }

        private void PreRender()
        {
            var renderPos = new Point(0, 0);

            _renderImage.BeginDrawing();
            _backgroundSprite.Color = Color.FromArgb(51, 56, 64);
            _backgroundSprite.Opacity = 240;
            _backgroundSprite.Draw(new Rectangle(renderPos, new Size(ClientArea.Width, ClientArea.Height)));

            var cornerTopLeft = _resourceManager.GetSprite("corner_top_left");
            cornerTopLeft.Draw(new Rectangle(renderPos.X, renderPos.Y, (int)cornerTopLeft.Width, (int)cornerTopLeft.Height));

            var cornerTopRight = _resourceManager.GetSprite("corner_top_right");
            cornerTopRight.Draw(new Rectangle(renderPos.X + ClientArea.Width - (int)cornerTopRight.Width, renderPos.Y, (int)cornerTopRight.Width, (int)cornerTopRight.Height));

            var borderTop = _resourceManager.GetSprite("border_top");
            borderTop.Draw(new Rectangle(renderPos.X + (int)cornerTopLeft.Width, renderPos.Y, ClientArea.Width - (int)cornerTopLeft.Width - (int)cornerTopRight.Width, (int)borderTop.Height));

            var cornerBottomLeft = _resourceManager.GetSprite("corner_bottom_left");
            cornerBottomLeft.Draw(new Rectangle(renderPos.X, renderPos.Y + ClientArea.Height - (int)cornerBottomLeft.Height, (int)cornerBottomLeft.Width, (int)cornerBottomLeft.Height));

            var cornerBottomRight = _resourceManager.GetSprite("corner_bottom_right");
            cornerBottomRight.Draw(new Rectangle(renderPos.X + ClientArea.Width - (int)cornerBottomRight.Width, renderPos.Y + ClientArea.Height - (int)cornerBottomRight.Height, (int)cornerBottomRight.Width, (int)cornerBottomRight.Height));

            var borderLeft = _resourceManager.GetSprite("border_left");
            borderLeft.Draw(new Rectangle(renderPos.X, renderPos.Y + (int)cornerTopLeft.Height, (int)borderLeft.Width, ClientArea.Height - (int)cornerBottomLeft.Height - (int)cornerTopLeft.Height));

            var borderRight = _resourceManager.GetSprite("border_right");
            borderRight.Draw(new Rectangle(renderPos.X + ClientArea.Width - (int)borderRight.Width, renderPos.Y + (int)cornerTopRight.Height, (int)borderRight.Width, ClientArea.Height - (int)cornerBottomRight.Height - (int)cornerTopRight.Height));

            var borderBottom = _resourceManager.GetSprite("border_bottom");
            borderBottom.Draw(new Rectangle(renderPos.X + (int)cornerTopLeft.Width, renderPos.Y + ClientArea.Height - (int)borderBottom.Height, ClientArea.Width - (int)cornerBottomLeft.Width - (int)cornerBottomRight.Width, (int)borderBottom.Height));

            var cornerMiddleLeft = _resourceManager.GetSprite("corner_middle_left");
            cornerMiddleLeft.Draw(new Rectangle(renderPos.X, renderPos.Y + ClientArea.Height - 16 - (int)cornerMiddleLeft.Height, (int)cornerMiddleLeft.Width, (int)cornerMiddleLeft.Height));

            var cornerMiddleRight = _resourceManager.GetSprite("corner_middle_right");
            cornerMiddleRight.Draw(new Rectangle(renderPos.X + ClientArea.Width - (int)cornerMiddleRight.Width, renderPos.Y + ClientArea.Height - 16 - (int)cornerMiddleRight.Height, (int)cornerMiddleRight.Width, (int)cornerMiddleRight.Height));

            var borderMiddle = _resourceManager.GetSprite("border_middle_h");
            borderMiddle.Draw(new Rectangle(renderPos.X + (int)cornerMiddleLeft.Width, renderPos.Y + ClientArea.Height - 16 - (int)borderMiddle.Height, ClientArea.Width - (int)cornerMiddleLeft.Width - (int)cornerMiddleRight.Width, (int)borderMiddle.Height));

            _renderImage.EndDrawing();
        }

        private IEnumerable<string> CheckInboundMessage(string message)
        {
            var lineList = new List<string>();

            if (_textInputLabel.Text.MeasureLine(message) < MaxLinePixelLength)
            {
                lineList.Add(message);
                return lineList;
            }

            var match = Regex.Match(message, @"^\[.+\]\s.+\:\s", RegexOptions.Singleline);
            var header = match.ToString();
            message = message.Substring(match.Length);

            var stringChunks = message.Split(new[] {' ', '-'}).ToList();
            var totalChunks = stringChunks.Count();
            var i = 0;

            lineList.Add(header);

            while (_textInputLabel.Text.MeasureLine(lineList[i]) < MaxLinePixelLength)
            {
                if (!stringChunks.Any()) break;

                if (_textInputLabel.Text.MeasureLine(lineList[i] + stringChunks.First()) < MaxLinePixelLength)
                {
                    lineList[i] += stringChunks.First() + " ";
                    stringChunks.RemoveAt(0);
                }
                else if ((i == 0 && totalChunks == stringChunks.Count()) ||
                    (lineList[i] == " " && _textInputLabel.Text.MeasureLine(stringChunks.First() + " ") > MaxLinePixelLength) ||
                    (_textInputLabel.Text.MeasureLine(lineList[i]) < MaxLinePixelLength && _textInputLabel.Text.MeasureLine(stringChunks.First() + " ") > MaxLinePixelLength))
                {
                    var largeWordChars = stringChunks.First().ToList();
                    stringChunks.RemoveAt(0);

                    while (_textInputLabel.Text.MeasureLine(lineList[i] + largeWordChars.First() + "-") < MaxLinePixelLength)
                    {
                        lineList[i] += largeWordChars.First();
                        largeWordChars.RemoveAt(0);
                    }

                    lineList[i] += "-";
                    lineList.Add(" " + new string(largeWordChars.ToArray()) + " ");
                    i++;
                }
                else
                {
                    lineList.Add(" ");
                    i++;
                }
            }

            return lineList;
        }

        public void AddLine(string message, ChatChannel channel)
        {
            if (_disposing) return;

            var messageSplit = CheckInboundMessage(message);

            foreach (var label in messageSplit.Select(part => new Label(part, _resourceManager)
                                                                  {
                                                                      Text =
                                                                          {
                                                                              Size = new Size(ClientArea.Width - 10, 12),
                                                                              Color = _chatColors[channel]
                                                                          }
                                                                  }))
            {
                _entries.Add(label);
            }

            DrawLines();
        }

        private void DrawLines()
        {
            CheckAndSetLine(_currentInputText.ToString());
            
            _textInputLabel.Position = new Point(ClientArea.X + 4, ClientArea.Y + ClientArea.Height - 20);
            _textInputLabel.Render();

            while (_entries.Count > MaxLines)
                _entries.RemoveAt(0);

            var start = Math.Max(0, _entries.Count - 12);

            for (var i = _entries.Count - 1; i >= start; i--)
            {
                _entries[i].Position = new Point(ClientArea.X + 2, ClientArea.Y + ClientArea.Height - (14 * (_entries.Count - i)) - 26);
                _entries[i].Render();
            }
        }

        private void CheckAndSetLine(string line)
        {
            // TODO: Refactor to use dynamic input box size.
            // Magic number is pixel width of chatbox input rectangle at
            // current resolution. Will need to modify once handling
            // different resolutions and scaling.
            if (_textInputLabel.Text.MeasureLine(line) > MaxLinePixelLength)
            {
                CheckAndSetLine(line.Substring(1));
            }
            else
            {
                _textInputLabel.Text.Text = line;
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
                if (TextSubmitted != null && !String.IsNullOrWhiteSpace(_currentInputText.ToString()))
                    TextSubmitted(this, _currentInputText.ToString());

                _currentInputText.Clear();
                _textInputLabel.Text.Text = "";

                Active = false;
                return true;
            }

            if (e.Key == KeyboardKeys.Back)
            {
                if (_currentInputText.Length > 0)
                    _currentInputText.Remove(_currentInputText.Length - 1, 1);
                return true;
            }

            if (char.IsLetterOrDigit(e.CharacterMapping.Character) || char.IsPunctuation(e.CharacterMapping.Character) || char.IsWhiteSpace(e.CharacterMapping.Character) || char.IsSymbol(e.CharacterMapping.Character))
            {
                _currentInputText.Append(e.Shift ? e.CharacterMapping.Shifted : e.CharacterMapping.Character);
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
            if (_disposing || !IsVisible()) return;

            _renderImage.Blit(ClientArea.X, ClientArea.Y);
            DrawLines();
        }

        public override void Resize()
        {
            PreRender();
        }
    }
}