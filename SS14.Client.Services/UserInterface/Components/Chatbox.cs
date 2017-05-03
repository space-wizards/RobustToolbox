using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.UserInterface;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SS14.Client.Services.UserInterface.Components
{
    public class Chatbox : ScrollableContainer
    {
        #region Delegates

        public delegate void TextSubmitHandler(Chatbox chatbox, string text);

        #endregion Delegates

        // private const int MaxHistory = 20;
        // private const int MaxLines = 10;
        private const int MaxLinePixelLength = 450;

        private readonly Dictionary<ChatChannel, SFML.Graphics.Color> _chatColors;

        private readonly IList<String> _inputHistory = new List<String>();

        private bool _active;
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
            }
        }

        public Chatbox(string uniqueName, Vector2i size, IResourceManager resourceManager) : base(uniqueName, size, resourceManager)
        {
            Position = new Vector2i(CluwneLib.CurrentClippingViewport.Width - Size.X - 10, 10);

            // ClientArea = new IntRect(Position.X, Position.Y, (int) Size.X, (int) Size.Y);

            input = new Textbox(Size.X, resourceManager)
            {
                drawColor = new Color(128, 128, 128, 100),
                textColor = new Color(255, 250, 240)
            };
            input.OnSubmit += Input_OnSubmit;

            _chatColors = new Dictionary<ChatChannel, Color>
            {
                [ChatChannel.Default] = new Color(200, 200, 200),
                [ChatChannel.Damage] = Color.Red,
                [ChatChannel.Radio] = new Color(0, 100, 0),
                [ChatChannel.Server] = Color.Blue,
                [ChatChannel.Player] = new Color(0, 128, 0),
                [ChatChannel.Lobby] = Color.White,
                [ChatChannel.Ingame] = new Color(0, 200, 0),
                [ChatChannel.OOC] = Color.White,
                [ChatChannel.Emote] = Color.Cyan,
                [ChatChannel.Visual] = Color.Yellow,
            };

            this.BackgroundColor = new Color(128, 128, 128, 100);
            this.DrawBackground = true;
            this.DrawBorder = true;
        }

        private bool Active
        {
            get { return _active; }
            set
            {
                _active = value;
                var manager = IoCManager.Resolve<IUserInterfaceManager>();
                if (value)
                {
                    manager.SetFocus(this);
                }
                else
                {
                    manager.RemoveFocus(this);
                }
            }
        }

        //
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

            bool atBottom = scrollbarV.Value >= scrollbarV.max;

            foreach (string content in CheckInboundMessage(message))
            {
                string[] subdivided = content.Split('\n');
                if (subdivided.Length == 0)
                    subdivided = new[] { content };

                foreach (string line in subdivided)
                {
                    var label = new Label(line, "CALIBRI", _resourceManager)
                    {
                        Position = new Vector2i(5, last_y),
                        Text =
                    {
                        Size = new Vector2i(ClientArea.Width - 10, 12),
                        Color = _chatColors[channel],
                    }
                    };
                    label.Update(0);
                    last_y = label.ClientArea.Bottom();
                    components.Add(label);
                }
            }

            if (atBottom)
            {
                Update(0);
                scrollbarV.Value = scrollbarV.max;
            }
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (e.Code == Keyboard.Key.T && !Active)
            {
                Active = true;
                ignoreFirstText = true;
                return true;
            }

            if (!Active)
            {
                return false;
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
            if (!Active)
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

        private void Input_OnSubmit(string text, Textbox sender)
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

            Active = false;
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
