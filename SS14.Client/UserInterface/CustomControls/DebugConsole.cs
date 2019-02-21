using SS14.Client.Console;
using SS14.Client.Interfaces.Console;
using SS14.Client.UserInterface.Controls;
using SS14.Client.Utility;
using SS14.Shared.Console;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SS14.Client.Input;
using SS14.Shared.Utility;

namespace SS14.Client.UserInterface.CustomControls
{
    // Quick note on how thread safety works in here:
    // Messages from other threads are not actually immediately drawn. They're stored in a queue.
    // Every frame OR the next time a message on the main Godot thread comes in, this queue is drained.
    // This keeps thread safety while still making it so messages are ordered how they come in.
    // And also if Update() stops firing due to an exception loop the console will still work.
    // (At least from the main thread, which is what's throwing the exceptions..)
    //
    // Disable reflection so that we won't be looked at for scene translation.
    [Reflect(false)]
    public class DebugConsole : Control, IDebugConsole
    {
        [Dependency] readonly IClientConsole console;

        private LineEdit CommandBar;

        // This one is used outside Godot.
        private OutputPanel Output;

        public IReadOnlyDictionary<string, IConsoleCommand> Commands => console.Commands;
        private readonly ConcurrentQueue<FormattedMessage> _messageQueue = new ConcurrentQueue<FormattedMessage>();

        private readonly List<string> CommandHistory = new List<string>();
        private int _historyPosition;
        private bool _currentCommandEdited;

        protected override ResourcePath ScenePath => new ResourcePath("/Scenes/DebugConsole/DebugConsole.tscn");

        protected override void Initialize()
        {
            IoCManager.InjectDependencies(this);

            Visible = false;

            CommandBar = GetChild<LineEdit>("CommandBar");
            CommandBar.OnKeyDown += CommandBarOnOnKeyDown;

            GetChild<RichTextLabel>("Contents").Dispose();

            var output = new OutputPanel {Name = "Contents"};
            output.SetAnchorAndMarginPreset(LayoutPreset.Wide, margin: 5);
            output.MarginBottom = -30;
            Output = output;
            AddChild(output);

            CommandBar.OnTextEntered += CommandEntered;
            CommandBar.OnTextChanged += CommandBarOnOnTextChanged;

            console.AddString += (_, args) => AddLine(args.Text, args.Channel, args.Color);
            console.AddFormatted += (_, args) => AddFormattedLine(args.Message);
            console.ClearText += (_, args) => Clear();
        }

        protected override void Update(ProcessFrameEventArgs args)
        {
            base.Update(args);

            _flushQueue();
        }

        public void Toggle()
        {
            var focus = CommandBar.HasKeyboardFocus();
            Visible = !Visible;
            if (Visible)
            {
                CommandBar.GrabKeyboardFocus();
            }
            else if (focus)
            {
                // We manually need to call this.
                // See https://github.com/godotengine/godot/pull/15074
                UserInterfaceManagerInternal.GDFocusExited(CommandBar);
            }
        }

        private void CommandEntered(LineEdit.LineEditEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.Text))
            {
                console.ProcessCommand(args.Text);
                CommandBar.Clear();
                if (CommandHistory.Count == 0 || CommandHistory[CommandHistory.Count - 1] != args.Text)
                {
                    _currentCommandEdited = false;
                    CommandHistory.Add(args.Text);
                    _historyPosition = CommandHistory.Count;
                }
            }
        }

        public void AddLine(string text, ChatChannel channel, Color color)
        {
            var formatted = new FormattedMessage(3);
            formatted.PushColor(color);
            formatted.AddText(text);
            formatted.Pop();
            AddFormattedLine(formatted);
        }

        public void AddLine(string text, Color color)
        {
            AddLine(text, ChatChannel.Default, color);
        }

        public void AddLine(string text)
        {
            AddLine(text, ChatChannel.Default, Color.White);
        }

        public void AddFormattedLine(FormattedMessage message)
        {
            if (!ThreadUtility.IsOnMainThread())
            {
                _messageQueue.Enqueue(message);
                return;
            }

            _flushQueue();
            _addFormattedLineInternal(message);
        }

        public void Clear()
        {
            Output.Clear();
        }

        private void _addFormattedLineInternal(FormattedMessage message)
        {
            Output.AddMessage(message);
        }

        private void _flushQueue()
        {
            DebugTools.Assert(ThreadUtility.IsOnMainThread());

            while (_messageQueue.TryDequeue(out var message))
            {
                _addFormattedLineInternal(message);
            }
        }

        private void CommandBarOnOnKeyDown(GUIKeyEventArgs obj)
        {
            switch (obj.Key)
            {
                case Keyboard.Key.Up:
                {
                    obj.Handle();
                    var current = CommandBar.Text;
                    if (!string.IsNullOrWhiteSpace(current) && _currentCommandEdited)
                    {
                        // Block up/down if something is typed in.
                        return;
                    }

                    if (_historyPosition <= 0)
                    {
                        return;
                    }

                    CommandBar.Text = CommandHistory[--_historyPosition];
                    break;
                }
                case Keyboard.Key.Down:
                {
                    obj.Handle();
                    var current = CommandBar.Text;
                    if (!string.IsNullOrWhiteSpace(current) && _currentCommandEdited)
                    {
                        // Block up/down if something is typed in.
                        return;
                    }

                    if (_historyPosition >= CommandHistory.Count - 1)
                    {
                        CommandBar.Text = "";
                        return;
                    }

                    CommandBar.Text = CommandHistory[++_historyPosition];
                    break;
                }
                case Keyboard.Key.PageDown:
                {
                    obj.Handle();
                    Output.ScrollToBottom();
                    break;
                }
            }
        }

        private void CommandBarOnOnTextChanged(LineEdit.LineEditEventArgs obj)
        {
            if (string.IsNullOrWhiteSpace(obj.Text))
            {
                _currentCommandEdited = false;
            }
            else
            {
                _currentCommandEdited = true;
            }
        }
    }
}
