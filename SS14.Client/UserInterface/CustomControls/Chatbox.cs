using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenTK.Graphics;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.Interfaces.Input;
using SS14.Client.Interfaces.Resource;
using SS14.Client.UserInterface.Components;
using SS14.Client.UserInterface.Controls;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.CustomControls
{
    public class Chatbox : ScrollableContainer
    {
        public delegate void TextSubmitHandler(Chatbox chatbox, string text);

        private const int MaxLinePixelLength = 500;

        private readonly Dictionary<ChatChannel, Color4> _chatColors;

        private readonly IList<string> _inputHistory = new List<string>();

        private bool _disposing;

        // To prevent the TextEntered from the key toggling chat being registered.
        private bool _ignoreFirstText;

        /// <summary>
        ///     Index while cycling through the input history. -1 means not going through history.
        /// </summary>
        private int _inputIndex = -1;

        /// <summary>
        ///     Message that WAS being input before going through history began.
        /// </summary>
        private string _inputTemp;

        private Textbox _input;

        private int _lastY;

        private bool _focus;

        private bool _keyBindingsEnabled;

        public override bool Focus
        {
            get => _focus;
            set
            {
                _focus = value;
                _input.Focus = value;
                KeyBindingsEnabled = !value;
            }
        }

        public bool KeyBindingsEnabled
        {
            get => _keyBindingsEnabled;
            set
            {
                //TODO: Disables all game input so that the chatbox can accept keys? wut?
                IoCManager.Resolve<IKeyBindingManager>().Enabled = value;
                _keyBindingsEnabled = value;
            }
        }

        public Chatbox(string uniqueName, Vector2i size, IResourceCache resourceCache) : base(uniqueName, size, resourceCache)
        {
            ScrollbarH.Visible = false;

            //Position = new Vector2i((int) CluwneLib.Window.Viewport.Width - Size.X - 10, 10);

            _input = new Textbox(Size.X)
            {
                BackgroundColor = new Color4(128, 128, 128, 100),
                ForegroundColor = new Color4(255, 250, 240, 255)
            };
            _input.OnSubmit += input_OnSubmit;

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

        public override bool KeyDown(KeyEventArgs e)
        {
            if (e.Key == Keyboard.Key.T && !Focus)
            {
                Focus = true;
                _ignoreFirstText = true;
                return true;
            }

            if (!Focus)
                return false;

            if (e.Key == Keyboard.Key.Escape)
            {
                Focus = false;
                return true;
            }

            if (e.Key == Keyboard.Key.Up)
            {
                if (_inputIndex == -1 && _inputHistory.Any())
                {
                    _inputTemp = _input.Text;
                    _inputIndex++;
                }
                else if (_inputIndex + 1 < _inputHistory.Count())
                {
                    _inputIndex++;
                }

                if (_inputIndex != -1)
                    _input.Text = _inputHistory[_inputIndex];

                return true;
            }

            if (e.Key == Keyboard.Key.Down)
            {
                if (_inputIndex == 0)
                {
                    _input.Text = _inputTemp;
                    _inputTemp = "";
                    _inputIndex--;
                }
                else if (_inputIndex != -1)
                {
                    _inputIndex--;
                    _input.Text = _inputHistory[_inputIndex];
                }

                return true;
            }

            return _input.KeyDown(e);
        }

        public override bool TextEntered(TextEventArgs e)
        {
            if (!Focus)
                return false;

            if (_ignoreFirstText)
            {
                _ignoreFirstText = false;
                return false;
            }
            return _input.TextEntered(e);
        }

        public override void Dispose()
        {
            _disposing = true;
            TextSubmitted = null;
            _input.Clear();
            _input = null;
            _chatColors.Clear();
            base.Dispose();
        }

        public override void DoLayout()
        {
            base.DoLayout();
            _input?.DoLayout();
        }

        protected override void OnCalcPosition()
        {
            base.OnCalcPosition();

            if (_input != null)
                _input.LocalPosition = Position + new Vector2i(ClientArea.Left, ClientArea.Bottom);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _input?.Update(frameTime);
        }

        public override void Draw()
        {
            if (_disposing || !Visible) return;
            CluwneLib.BlendingMode = BlendingModes.Modulated;
            var clientRect = ClientArea.Translated(Position);
            CluwneLib.drawRectangle(clientRect.Left, clientRect.Top, clientRect.Width, clientRect.Height, new Color4(0, 0, 0, 100));
            CluwneLib.drawRectangle(clientRect.Left, clientRect.Top, clientRect.Width, clientRect.Height, new Color4(211, 211, 211, 100));
            CluwneLib.BlendingMode = BlendingModes.None;
            base.Draw();
            _input.Draw();
        }

        public event TextSubmitHandler TextSubmitted;

        private IEnumerable<string> CheckInboundMessage(string message)
        {
            var lineList = new List<string>();

            var text = _input;
            if (text.MeasureLine(message) < MaxLinePixelLength)
            {
                lineList.Add(message);
                return lineList;
            }

            var match = Regex.Match(message, @"^\[.+\]\s.+\:\s", RegexOptions.Singleline);
            var header = match.ToString();
            message = message.Substring(match.Length);

            var stringChunks = message.Split(' ', '-').ToList();
            var totalChunks = stringChunks.Count();
            var i = 0;

            lineList.Add(header);

            while (text.MeasureLine(lineList[i]) < MaxLinePixelLength)
            {
                if (!stringChunks.Any()) break;

                if (text.MeasureLine(lineList[i] + stringChunks.First()) < MaxLinePixelLength)
                {
                    lineList[i] += stringChunks.First() + " ";
                    stringChunks.RemoveAt(0);
                }
                else if (i == 0 && totalChunks == stringChunks.Count() ||
                         lineList[i] == " " &&
                         text.MeasureLine(stringChunks.First() + " ") > MaxLinePixelLength ||
                         text.MeasureLine(lineList[i]) < MaxLinePixelLength &&
                         text.MeasureLine(stringChunks.First() + " ") > MaxLinePixelLength)
                {
                    var largeWordChars = stringChunks.First().ToList();
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

            var lineHeight = 12;

            var atBottom = ScrollbarV.Value >= ScrollbarV.max;

            foreach (var content in CheckInboundMessage(message))
            {
                var label = new Label(content, "CALIBRI")
                {
                    LocalPosition = new Vector2i(5, _lastY),
                    Size = new Vector2i(ClientArea.Width - 10, lineHeight),
                    ForegroundColor = _chatColors[channel],
                };
                label.Update(0);
                _lastY = label.ClientArea.Bottom;
                Components.Add(label);

                // If the message had newlines adjust the bottom to fix the extra lines
                if (message.Split('\n').Length > 0)
                    _lastY += lineHeight * (message.Split('\n').Length - 1);
            }

            if (atBottom)
            {
                Update(0);
                ScrollbarV.Value = ScrollbarV.max;
            }
        }

        private void CheckAndSetLine(string line)
        {
            var text = _input;

            // TODO: Refactor to use dynamic input box size.
            // Magic number is pixel width of chatbox input rectangle at
            // current resolution. Will need to modify once handling
            // different resolutions and scaling.
            if (text.MeasureLine(line) > MaxLinePixelLength)
                CheckAndSetLine(line.Substring(1));
            else
                text.Text = line;
        }

        private void input_OnSubmit(string text, Textbox sender)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                TextSubmitted(this, text);
                _inputHistory.Insert(0, text);
            }

            _inputIndex = -1;

            Focus = false;
        }
    }
}
