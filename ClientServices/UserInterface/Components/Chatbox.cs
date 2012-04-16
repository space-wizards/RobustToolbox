using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using ClientInterfaces;
using ClientInterfaces.Input;
using ClientInterfaces.Resource;
using ClientInterfaces.UserInterface;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using GorgonLibrary.InputDevices;
using SS13_Shared;

namespace ClientServices.UserInterface.Components
{
    public class Chatbox : GuiComponent
    {
        private const int MaxHistory = 20;
        private const int MaxLines = 10;
        private const int MaxLinePixelLength = 450;

        private readonly IResourceManager _resourceManager;
        private readonly IUserInterfaceManager _userInterfaceManager;
        private readonly IKeyBindingManager _keyBindingManager;

        public delegate void TextSubmitHandler(Chatbox chatbox, string text);
        public event TextSubmitHandler TextSubmitted;

        private readonly IList<Label> _entries = new List<Label>();
        private readonly IList<String> _inputHistory = new List<String>();
        private readonly StringBuilder _currentInputText = new StringBuilder();
        private readonly Dictionary<ChatChannel, Color> _chatColors;

        private Label _textInputLabel;

        private string _inputTemp;
        private int _inputIndex = -1;
        private bool _disposing;
        private bool _active;
        private bool Active
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

            const int width = 475;
            const int height = 175;

            Position = new Point(Gorgon.CurrentClippingViewport.Width - width - 10, 10);

            ClientArea = new Rectangle(Position.X, Position.Y, width, height);

            _textInputLabel = new Label("", "CALIBRI", _resourceManager)
                                {
                                    Text =
                                        {
                                            Size = new Size(ClientArea.Width - 10, 12),
                                            Color = Color.Green
                                        }      
                                };

            _chatColors = new Dictionary<ChatChannel, Color>
                            {
                                {ChatChannel.Default, Color.Gray},
                                {ChatChannel.Damage, Color.Red},
                                {ChatChannel.Radio, Color.DarkGreen},
                                {ChatChannel.Server, Color.Blue},
                                {ChatChannel.Player, Color.Green},
                                {ChatChannel.Lobby, Color.White},
                                {ChatChannel.Ingame, Color.Green},
                                {ChatChannel.OOC, Color.White},
                                {ChatChannel.Emote, Color.Cyan},
                                {ChatChannel.Visual, Color.Yellow},
                            };
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

            foreach (var label in messageSplit.Select(part => new Label(part, "CALIBRI", _resourceManager)
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
            
            _textInputLabel.Position = new Point(ClientArea.X + 4, ClientArea.Y + ClientArea.Height - 23);
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
                {
                    TextSubmitted(this, _currentInputText.ToString());
                    _inputHistory.Insert(0, _currentInputText.ToString());

                    while (_inputHistory.Count() > MaxHistory)
                    {
                        _inputHistory.RemoveAt(MaxHistory);
                    }
                }

                _inputIndex = -1;
                _currentInputText.Clear();

                Active = false;
                return true;
            }

            if (e.Key == KeyboardKeys.Up)
            {
                if (_inputIndex == -1 && _inputHistory.Any())
                {
                    _inputTemp = _currentInputText.ToString();
                    _inputIndex++;
                }
                else if(_inputIndex + 1 < _inputHistory.Count())
                {
                    _inputIndex++;
                }


                if (_inputIndex != -1)
                {
                    _currentInputText.Clear();
                    _currentInputText.Append(_inputHistory[_inputIndex]);
                }

                return true;
            }

            if (e.Key == KeyboardKeys.Down)
            {
                if (_inputIndex == 0)
                {
                    _currentInputText.Clear();
                    _currentInputText.Append(_inputTemp);
                    _inputTemp = "";
                    _inputIndex--;
                }
                else if (_inputIndex != -1)
                {
                    _inputIndex--;
                    _currentInputText.Clear();
                    _currentInputText.Append(_inputHistory[_inputIndex]);
                }
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
            Gorgon.CurrentRenderTarget.BlendingMode = BlendingModes.Modulated;
            Gorgon.CurrentRenderTarget.FilledRectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, Color.FromArgb(100, Color.Black));
            Gorgon.CurrentRenderTarget.Rectangle(ClientArea.X, ClientArea.Y, ClientArea.Width, ClientArea.Height, Color.FromArgb(100, Color.LightGray));
            Gorgon.CurrentRenderTarget.BlendingMode = BlendingModes.None;
            DrawLines();
        }
    }
}