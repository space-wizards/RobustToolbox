using OpenTK.Graphics;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Utility;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.Components
{
    public class Chatbox : ScrollableContainer
    {
        #region Delegates

        public delegate void TextSubmitHandler(Chatbox chatbox, string text);

        #endregion Delegates

        private const int MaxLinePixelLength = 500;

        private readonly Dictionary<ChatChannel, Color4> _chatColors;

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
            ScrollbarH.SetVisible(false);

            Position = new Vector2i((int)CluwneLib.Window.Viewport.Width - (int)Size.X - 10, 10);

            input = new Textbox(Size.X)
            {
                BackgroundColor = new Color4(128, 128, 128, 100),
                ForegroundColor = new Color4(255, 250, 240, 255)
            };
            input.OnSubmit += new Textbox.TextSubmitHandler(input_OnSubmit);

            _chatColors = new Dictionary<ChatChannel, Color4>
            {
                [ChatChannel.Default] = Color4.Gray,
                [ChatChannel.Damage] = Color4.Red,
                [ChatChannel.Radio] = new Color4(0, 100, 0, 255),
                [ChatChannel.Server] = Color4.Blue,
                [ChatChannel.Player] = new Color4(0, 128, 0, 255),
                [ChatChannel.Lobby] = Color4.White,
                [ChatChannel.Ingame] = new Color4(0, 200, 0, 255),
                [ChatChannel.OOC] = Color4.White,
                [ChatChannel.Emote] = Color4.Cyan,
                [ChatChannel.Visual] = Color4.Yellow,
            };

            BackgroundColor = new Color4(128, 128, 128, 100);
            DrawBackground = true;
            DrawBorder = true;
        }

        private IEnumerable<string> CheckInboundMessage(string message)
        {
            var lineList = new List<string>();

            var text = input;
            if (text.MeasureLine(message) < MaxLinePixelLength)
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

            while (text.MeasureLine(lineList[i]) < MaxLinePixelLength)
            {
                if (!stringChunks.Any()) break;

                if (text.MeasureLine(lineList[i] + stringChunks.First()) < MaxLinePixelLength)
                {
                    lineList[i] += stringChunks.First() + " ";
                    stringChunks.RemoveAt(0);
                }
                else if ((i == 0 && totalChunks == stringChunks.Count()) ||
                         (lineList[i] == " " &&
                          text.MeasureLine(stringChunks.First() + " ") > MaxLinePixelLength) ||
                         (text.MeasureLine(lineList[i]) < MaxLinePixelLength &&
                          text.MeasureLine(stringChunks.First() + " ") > MaxLinePixelLength))
                {
                    List<char> largeWordChars = stringChunks.First().ToList();
                    stringChunks.RemoveAt(0);

                    while (text.MeasureLine(lineList[i] + largeWordChars.First() + "-") <
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

            bool atBottom = ScrollbarV.Value >= ScrollbarV.max;

            foreach (string content in CheckInboundMessage(message))
            {
                var label = new Label(content, "CALIBRI")
                {
                    LocalPosition = new Vector2i(5, last_y),
                    Size = new Vector2i(ClientArea.Width - 10, lineHeight),
                    ForegroundColor = _chatColors[channel],
                };
                label.Update(0);
                last_y = label.ClientArea.Bottom;
                Components.Add(label);

                // If the message had newlines adjust the bottom to fix the extra lines
                if (message.Split('\n').Length > 0)
                {
                    last_y += lineHeight * (message.Split('\n').Length - 1);
                }
            }

            if (atBottom)
            {
                Update(0);
                ScrollbarV.Value = ScrollbarV.max;
            }
        }

        private void CheckAndSetLine(string line)
        {
            var text = input;

            // TODO: Refactor to use dynamic input box size.
            // Magic number is pixel width of chatbox input rectangle at
            // current resolution. Will need to modify once handling
            // different resolutions and scaling.
            if (text.MeasureLine(line) > MaxLinePixelLength)
            {
                CheckAndSetLine(line.Substring(1));
            }
            else
            {
                text.Text = line;
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
                input.Position = new Vector2i(ClientArea.Left, ClientArea.Bottom);
                input.Update(frameTime);
            }
        }

        public override void Draw()
        {
            if (_disposing || !IsVisible()) return;
            CluwneLib.BlendingMode = BlendingModes.Modulated;
            CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, new Color4(0, 0, 0, 100));
            CluwneLib.drawRectangle(ClientArea.Left, ClientArea.Top, ClientArea.Width, ClientArea.Height, new Color4(211, 211, 211, 100));
            CluwneLib.BlendingMode = BlendingModes.None;
            base.Draw();
            input.Draw();
        }
    }
}
