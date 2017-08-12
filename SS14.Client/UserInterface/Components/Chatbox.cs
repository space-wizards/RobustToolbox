using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vector2i = SFML.System.Vector2i;

namespace SS14.Client.UserInterface.Components
{

    public class Chatbox : ScrollableContainer
    {
        #region Delegates

        public delegate void TextSubmitHandler(Chatbox chatbox, string text);

        #endregion Delegates

        // private const int MaxHistory = 20;
        // private const int MaxLines = 10;
        private const int MaxLinePixelLength = 500;

        private readonly Dictionary<ChatChannel, SFML.Graphics.Color> _chatColors;

        private readonly IList<String> _inputHistory = new List<String>();

        private bool _disposing;

        // To prevent the TextEntered from the key toggling chat being registered.
        private bool ignoreFirstText = false;

        /// <summary>
        /// Index while cycling through the input history. -1 means not going through history.
        /// </summary>
        private int _inputIndex = -1;

        /// <summary>
        /// Message that WAS being input before going through history began.
        /// </summary>
        private string _inputTemp;

        private Textbox input;

        private int last_y = 0;

        public event TextSubmitHandler TextSubmitted;

        private bool _focus;

        public override bool Focus
        {
            get
            {
                return _focus;
            }
            set
            {
                _focus = value;
                input.Focus = value;
                KeyBindingsEnabled = !value;
            }
        }

        private bool _keyBindingsEnabled;
        public bool KeyBindingsEnabled
        {
            get { return _keyBindingsEnabled; }
            set
            {
                IoCManager.Resolve<IKeyBindingManager>().Enabled = value;
                _keyBindingsEnabled = value;
            }
        }

        public Chatbox(string uniqueName, Vector2i size, IResourceCache resourceCache) : base(uniqueName, size, resourceCache)
        {
            scrollbarH.SetVisible(false);

            Position = new Vector2i((int)CluwneLib.CurrentClippingViewport.Width - (int)Size.X - 10, 10);

            // ClientArea = new IntRect(Position.X, Position.Y, (int) Size.X, (int) Size.Y);

            input = new Textbox(Size.X, resourceCache)
            {
                drawColor = new SFML.Graphics.Color(128, 128, 128, 100),
                textColor = new SFML.Graphics.Color(255, 250, 240)
            };
            input.OnSubmit += new Textbox.TextSubmitHandler(input_OnSubmit);

            _chatColors = new Dictionary<ChatChannel, SFML.Graphics.Color>
            {
                [ChatChannel.Default] = new SFML.Graphics.Color(128, 128, 128),
                [ChatChannel.Damage] = Color.Red,
                [ChatChannel.Radio] = new SFML.Graphics.Color(0, 100, 0),
                [ChatChannel.Server] = Color.Blue,
                [ChatChannel.Player] = new SFML.Graphics.Color(0, 128, 0),
                [ChatChannel.Lobby] = Color.White,
                [ChatChannel.Ingame] = new SFML.Graphics.Color(0, 200, 0),
                [ChatChannel.OOC] = Color.White,
                [ChatChannel.Emote] = Color.Cyan,
                [ChatChannel.Visual] = Color.Yellow,
            };

            BackgroundColor = new SFML.Graphics.Color(128, 128, 128, 100);
            DrawBackground = true;
            DrawBorder = true;
        }

        private IEnumerable<string> CheckInboundMessage(string message)
        {
            var lineList = new List<string>();

            if (input.Label.MeasureLine(message) < MaxLinePixelLength)
            {
                lineList.Add(message);
                return lineList;
            }

            Match match = Regex.Match(message, @"^\[.+\]\s.+\:\s", RegexOptions.Singleline);
            string header = match.ToString();
            message = message.Substring(match.Length);

            List<string> stringChunks = message.Split(new[] { ' ', '-' }).ToList();
            int totalChunks = stringChunks.Count();
            int i = 0;

            lineList.Add(header);

            while (input.Label.MeasureLine(lineList[i]) < MaxLinePixelLength)
            {
                if (!stringChunks.Any()) break;

                if (input.Label.MeasureLine(lineList[i] + stringChunks.First()) < MaxLinePixelLength) 
                {
                    lineList[i] += stringChunks.First() + " ";
                    stringChunks.RemoveAt(0);
                }
                else if ((i == 0 && totalChunks == stringChunks.Count()) ||
                         (lineList[i] == " " &&
                          input.Label.MeasureLine(stringChunks.First() + " ") > MaxLinePixelLength) ||
                         (input.Label.MeasureLine(lineList[i]) < MaxLinePixelLength &&
                          input.Label.MeasureLine(stringChunks.First() + " ") > MaxLinePixelLength))
                {
                    List<char> largeWordChars = stringChunks.First().ToList();
                    stringChunks.RemoveAt(0);

                    while (input.Label.MeasureLine(lineList[i] + largeWordChars.First() + "-") <
                           MaxLinePixelLength)
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

            int lineHeight = 12;

            bool atBottom = scrollbarV.Value >= scrollbarV.max;

            foreach (string content in CheckInboundMessage(message))
            {
                var label = new Label(content, "CALIBRI", _resourceCache)
                {
                    Position = new Vector2i(5, last_y),
                    Text =
                    {
                        Size = new Vector2i(ClientArea.Width - 10, lineHeight),
                        Color = _chatColors[channel],
                    }
                };
                label.Update(0);
                last_y = label.ClientArea.Bottom();
                components.Add(label);

                // If the message had newlines adjust the bottom to fix the extra lines
                if (message.Split('\n').Length > 0)
                {
                    last_y += lineHeight * (message.Split('\n').Length - 1);
                }
            }

            if (atBottom)
            {
                Update(0);
                scrollbarV.Value = scrollbarV.max;
            }
        }

        private void CheckAndSetLine(string line)
        {
            // TODO: Refactor to use dynamic input box size.
            // Magic number is pixel width of chatbox input rectangle at
            // current resolution. Will need to modify once handling
            // different resolutions and scaling.
            if (input.Label.MeasureLine(line) > MaxLinePixelLength)
            {
                CheckAndSetLine(line.Substring(1));
            }
            else
            {
                input.Label.Text = line;
            }
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.T && !Focus)
            {
                Focus = true;
                ignoreFirstText = true;
                return true;
            }

            if (!Focus)
            {
                return false;
            }

            if (e.Code == Keyboard.Key.Escape)
            {
                Focus = false;
                return true;
            }

            if (e.Code == Keyboard.Key.Up)
            {
                if (_inputIndex == -1 && _inputHistory.Any())
                {
                    _inputTemp = input.Text.ToString();
                    _inputIndex++;
                }
                else if (_inputIndex + 1 < _inputHistory.Count())
                {
                    _inputIndex++;
                }

                if (_inputIndex != -1)
                {
                    input.Text = _inputHistory[_inputIndex];
                }

                return true;
            }

            if (e.Code == Keyboard.Key.Down)
            {
                if (_inputIndex == 0)
                {
                    input.Text = _inputTemp;
                    _inputTemp = "";
                    _inputIndex--;
                }
                else if (_inputIndex != -1)
                {
                    _inputIndex--;
                    input.Text = _inputHistory[_inputIndex];
                }

                return true;
            }

            return input.KeyDown(e);
        }

        public override bool TextEntered(TextEventArgs e)
        {
            if (!Focus)
            {
                return false;
            }

            if (ignoreFirstText)
            {
                ignoreFirstText = false;
                return false;
            }
            return input.TextEntered(e);
        }

        private void input_OnSubmit(string text, Textbox sender)
        {
            if (!String.IsNullOrWhiteSpace(text))
            {
                TextSubmitted(this, text);
                _inputHistory.Insert(0, text);

                /*
                while (_inputHistory.Count() > MaxHistory)
                {
                    _inputHistory.RemoveAt(MaxHistory);
                }
                */
            }

            _inputIndex = -1;

            Focus = false;
        }

        public override void Dispose()
        {
            _disposing = true;
            TextSubmitted = null;
            input.Clear();
            input = null;
            _chatColors.Clear();
            base.Dispose();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            if (input != null)
            {
                input.Position = new Vector2i(ClientArea.Left, ClientArea.Bottom());
                input.Update(frameTime);
            }
        }

        public override void Render()
        {
            if (_disposing || !IsVisible()) return;
            CluwneLib.BlendingMode = BlendingModes.Modulated;
            CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, new SFML.Graphics.Color(0, 0, 0, 100));
            CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, new SFML.Graphics.Color(211, 211, 211, 100));
            CluwneLib.BlendingMode = BlendingModes.None;
            base.Render();
            input.Render();
        }
    }
}
