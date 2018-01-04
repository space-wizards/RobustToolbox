using System.Collections.Generic;
using System.Linq;
using SS14.Client.Console;
using SS14.Client.Input;
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

        private bool firstLine = true;
        public LineEdit Input { get; private set; }
        private RichTextLabel contents;

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

        public Chatbox() : base()
        {
        }

        protected override Godot.Control SpawnSceneControl()
        {
            var res = (Godot.PackedScene)Godot.ResourceLoader.Load("res://Scenes/ChatBox/ChatBox.tscn");
            return (Godot.Control)res.Instance();
        }

        protected override void Initialize()
        {
            base.Initialize();

            Input = GetChild<LineEdit>("Input");
            Input.OnKeyDown += InputKeyDown;
            Input.OnTextEntered += Input_OnTextEntered;
            contents = GetChild<RichTextLabel>("Contents");
        }

        protected override void MouseDown(GUIMouseButtonEventArgs e)
        {
            base.MouseDown(e);

            Input.GrabFocus();
        }

        private void InputKeyDown(GUIKeyEventArgs e)
        {
            if (e.Key == Keyboard.Key.Escape)
            {
                Input.ReleaseFocus();
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
                return;
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

            if (!firstLine)
            {
                contents.NewLine();
            }
            else
            {
                firstLine = false;
            }
            contents.PushColor(color);
            contents.AddText(message);
            contents.Pop(); // Pop the color off.
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
            Input.ReleaseFocus();
        }

        public void AddLine(object sender, AddStringArgs e)
        {
            AddLine(e.Text, e.Channel, e.Color);
        }
    }
}
