using System.Collections.Generic;
using Robust.Client.Console;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    public class Chatbox : PanelContainer
    {
        protected override ResourcePath ScenePath => new ResourcePath("/Scenes/ChatBox/ChatBox.tscn");

        public delegate void TextSubmitHandler(Chatbox chatbox, string text);

        private const int MaxLinePixelLength = 500;

        private readonly IList<string> _inputHistory = new List<string>();

        public LineEdit Input { get; private set; }
        private OutputPanel contents;

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
        public List<ChatChannel> ChannelBlacklist { get; set; } = new List<ChatChannel>()
        {
            ChatChannel.Default,
        };

        protected override void Initialize()
        {
            base.Initialize();

            Input = GetChild<LineEdit>("VBoxContainer/Input");
            Input.OnKeyDown += InputKeyDown;
            Input.OnTextEntered += Input_OnTextEntered;
            GetChild<Control>("VBoxContainer/Contents").Dispose();

            contents = new OutputPanel
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
            };
            GetChild("VBoxContainer").AddChild(contents);
            contents.SetPositionInParent(0);

            if (!GameController.OnGodot)
            {
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.Gray.WithAlpha(0.5f)};
            }
        }

        protected internal override void MouseDown(GUIMouseButtonEventArgs e)
        {
            base.MouseDown(e);

            Input.GrabKeyboardFocus();
        }

        private void InputKeyDown(GUIKeyEventArgs e)
        {
            if (e.Key == Keyboard.Key.Escape)
            {
                Input.ReleaseKeyboardFocus();
                e.Handle();
                return;
            }

            if (e.Key == Keyboard.Key.Up)
            {
                if (_inputIndex == -1 && _inputHistory.Count != 0)
                {
                    _inputTemp = Input.Text;
                    _inputIndex++;
                }
                else if (_inputIndex + 1 < _inputHistory.Count)
                {
                    _inputIndex++;
                }

                if (_inputIndex != -1)
                {
                    Input.Text = _inputHistory[_inputIndex];
                }

                e.Handle();
                return;
            }

            if (e.Key == Keyboard.Key.Down)
            {
                if (_inputIndex == 0)
                {
                    Input.Text = _inputTemp;
                    _inputTemp = "";
                    _inputIndex--;
                }
                else if (_inputIndex != -1)
                {
                    _inputIndex--;
                    Input.Text = _inputHistory[_inputIndex];
                }

                e.Handle();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                TextSubmitted = null;
                Input = null;
                contents = null;
            }
        }

        public event TextSubmitHandler TextSubmitted;

        public void AddLine(string message, ChatChannel channel, Color color)
        {
            if (Disposed)
            {
                return;
            }

            if (ChannelBlacklist.Contains(channel))
                return;

            var formatted = new FormattedMessage(3);
            formatted.PushColor(color);
            formatted.AddText(message);
            formatted.Pop();
            contents.AddMessage(formatted);
        }

        public void AddLine(string text, Color color)
        {
            AddLine(text, ChatChannel.Default, color);
        }

        private void Input_OnTextEntered(LineEdit.LineEditEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.Text))
            {
                TextSubmitted?.Invoke(this, args.Text);
                _inputHistory.Insert(0, args.Text);
            }

            _inputIndex = -1;

            Input.Clear();
            Input.ReleaseKeyboardFocus();
        }

        public void AddLine(object sender, AddStringArgs e)
        {
            AddLine(e.Text, e.Channel, e.Color);
        }
    }
}
