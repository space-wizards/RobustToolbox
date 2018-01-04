
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpenTK.Graphics;
using SS14.Client.Console;
using SS14.Client.Graphics.Input;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Console;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.CustomControls
{
    public class Chatbox : Panel
    {
        public delegate void TextSubmitHandler(Chatbox chatbox, string text);

        private const int MaxLinePixelLength = 500;

        private readonly IList<string> _inputHistory = new List<string>();

        private readonly Textbox _input;
        private readonly ScrollableContainer _historyBox;
        private readonly ListPanel _chatHistoryList;

        /// <summary>
        ///     To prevent the TextEntered from the key toggling chat being registered.
        /// </summary>
        private bool _ignoreFirstText;

        /// <summary>
        ///     Index while cycling through the input history. -1 means not going through history.
        /// </summary>
        private int _inputIndex = -1;

        /// <summary>
        ///     Message that WAS being input before going through history began.
        /// </summary>
        private string _inputTemp;

        /// <summary>
        ///     Default formatting string for the ClientChatConsole.
        /// </summary>
        public string DefaultChatFormat { get; set; }

        /// <summary>
        ///     Blacklists channels from being displayed.
        /// </summary>
        public List<ChatChannel> ChannelBlacklist { get; set; }

        public override bool Focus
        {
            get => _input.Focus;
            set => _input.Focus = value;
        }

        public Chatbox(Vector2i size)
        {
            BackgroundColor = new Color4(128, 128, 128, 128);
            BorderColor = new Color4(0, 0, 0, 128);
            DrawBackground = true;
            DrawBorder = true;

            Size = size;

            _historyBox = new ScrollableContainer(size);
            AddControl(_historyBox);

            _chatHistoryList = new ListPanel();
            _historyBox.Container.AddControl(_chatHistoryList);

            _input = new Textbox(Size.X)
            {
                BackgroundColor = new Color4(128, 128, 128, 128),
                ForegroundColor = new Color4(255, 250, 240, 255)
            };
            _input.OnSubmit += (sender, text) => input_OnSubmit(sender, text);

            ChannelBlacklist = new List<ChatChannel>()
            {
                ChatChannel.Default,
            };
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (base.MouseDown(e))
                return true;

            return _input.MouseDown(e);
        }

        public override bool KeyDown(KeyEventArgs e)
        {
            if (e.Key == Keyboard.Key.T && !Focus && UiManager.HasFocus(null))
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
            base.Dispose();

            TextSubmitted = null;
            _input.Dispose();
        }

        public override void DoLayout()
        {
            base.DoLayout();
            _input.Width = ClientArea.Width;
            _input.DoLayout();
        }

        protected override void OnCalcPosition()
        {
            base.OnCalcPosition();

            if (_input != null)
                _input.LocalPosition = Position + new Vector2i(ClientArea.Left, ClientArea.Bottom + 1);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _input.Update(frameTime);
        }

        public override void Draw()
        {
            if (Disposed || !Visible) return;
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

        public void AddLine(string message, ChatChannel channel, Color color)
        {
            if (Disposed) return;

            if(ChannelBlacklist.Contains(channel))
                return;

            //TODO: LineHeight should be from the Font, not hard coded.
            const int lineHeight = 12;

            var scrollbarV = _historyBox.ScrollbarV;
            var atBottom = scrollbarV.Value >= scrollbarV.Max;

            foreach (var content in CheckInboundMessage(message))
            {
                _chatHistoryList.AddControl(new Label(content, "CALIBRI")
                {
                    Size = new Vector2i(ClientArea.Width - 10, lineHeight),
                    ForegroundColor = color,
                });
            }

            // always layout the control after changing things
            _historyBox.DoLayout();

            if (atBottom)
            {
                Update(0);
                scrollbarV.Value = scrollbarV.Max;
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

        private void input_OnSubmit(Textbox sender, string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                TextSubmitted?.Invoke(this, text);
                _inputHistory.Insert(0, text);
            }

            _inputIndex = -1;

            Focus = false;
        }

        public void AddLine(object sender, AddStringArgs e)
        {
            AddLine(e.Text, e.Channel, e.Color);
        }
    }
}
