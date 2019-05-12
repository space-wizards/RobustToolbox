using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Robust.Client.Console;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Input;
using Robust.Client.Interfaces.Console;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Interfaces.Resources;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    // Quick note on how thread safety works in here:
    // Messages from other threads are not actually immediately drawn. They're stored in a queue.
    // Every frame OR the next time a message on the main thread comes in, this queue is drained.
    // This keeps thread safety while still making it so messages are ordered how they come in.
    // And also if Update() stops firing due to an exception loop the console will still work.
    // (At least from the main thread, which is what's throwing the exceptions..)
    public class DebugConsole : Control, IDebugConsole
    {
        private const int MaxHistorySize = 100;

        private readonly IClientConsole _console;
        private readonly IResourceManager _resourceManager;

        private static readonly ResourcePath HistoryPath = new ResourcePath("/debug_console_history.json");

        private LineEdit CommandBar;
        private OutputPanel Output;

        public IReadOnlyDictionary<string, IConsoleCommand> Commands => _console.Commands;
        private readonly ConcurrentQueue<FormattedMessage> _messageQueue = new ConcurrentQueue<FormattedMessage>();

        private readonly List<string> CommandHistory = new List<string>();
        private int _historyPosition;
        private bool _currentCommandEdited;

        public DebugConsole(IClientConsole console, IResourceManager resMan)
        {
            _console = console;
            _resourceManager = resMan;

            PerformLayout();
        }

        private void PerformLayout()
        {
            Visible = false;

            AnchorRight = 1f;
            AnchorBottom = 0.35f;

            var boxContainer = new VBoxContainer {SeparationOverride = 0};
            boxContainer.SetAnchorPreset(LayoutPreset.Wide);
            AddChild(boxContainer);
            var styleBox = new StyleBoxFlat
            {
                BackgroundColor = Color.Gray.WithAlpha(0.5f),
            };
            styleBox.SetContentMarginOverride(StyleBox.Margin.All, 3);
            Output = new OutputPanel
            {
                SizeFlagsVertical = SizeFlags.FillExpand,
                StyleBoxOverride = styleBox
            };
            boxContainer.AddChild(Output);

            CommandBar = new LineEdit {PlaceHolder = "Command Here"};
            boxContainer.AddChild(CommandBar);
            CommandBar.OnKeyDown += CommandBarOnOnKeyDown;
            CommandBar.OnTextEntered += CommandEntered;
            CommandBar.OnTextChanged += CommandBarOnOnTextChanged;

            _console.AddString += (_, args) => AddLine(args.Text, args.Color);
            _console.AddFormatted += (_, args) => AddFormattedLine(args.Message);
            _console.ClearText += (_, args) => Clear();

            _loadHistoryFromDisk();
        }

        protected override void Update(ProcessFrameEventArgs args)
        {
            base.Update(args);

            _flushQueue();
        }

        public void Toggle()
        {
            Visible = !Visible;
            if (Visible)
            {
                CommandBar.IgnoreNext = true;
                CommandBar.GrabKeyboardFocus();
            }
        }

        private void CommandEntered(LineEdit.LineEditEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.Text))
            {
                _console.ProcessCommand(args.Text);
                CommandBar.Clear();
                if (CommandHistory.Count == 0 || CommandHistory[CommandHistory.Count - 1] != args.Text)
                {
                    _currentCommandEdited = false;
                    CommandHistory.Add(args.Text);
                    if (CommandHistory.Count > MaxHistorySize)
                    {
                        CommandHistory.RemoveAt(0);
                    }
                    _historyPosition = CommandHistory.Count;
                    _flushHistoryToDisk();
                }
            }
        }

        public void AddLine(string text, Color color)
        {
            var formatted = new FormattedMessage(3);
            formatted.PushColor(color);
            formatted.AddText(text);
            formatted.Pop();
            AddFormattedLine(formatted);
        }

        public void AddLine(string text)
        {
            AddLine(text, Color.White);
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

                    if (++_historyPosition >= CommandHistory.Count)
                    {
                        CommandBar.Text = "";
                        _historyPosition = CommandHistory.Count;
                        return;
                    }

                    CommandBar.Text = CommandHistory[_historyPosition];
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

        private void _loadHistoryFromDisk()
        {
            CommandHistory.Clear();
            Stream stream;
            try
            {
                stream = _resourceManager.UserData.Open(HistoryPath, FileMode.Open);
            }
            catch (FileNotFoundException)
            {
                // Nada, nothing to load in that case.
                return;
            }

            try
            {
                using (var reader = new StreamReader(stream, EncodingHelpers.UTF8))
                {
                    var data = JsonConvert.DeserializeObject<List<string>>(reader.ReadToEnd());
                    CommandHistory.Clear();
                    CommandHistory.AddRange(data);
                    _historyPosition = CommandHistory.Count;
                }
            }
            finally
            {
                stream?.Dispose();
            }
        }

        private void _flushHistoryToDisk()
        {
            using (var stream = _resourceManager.UserData.Open(HistoryPath, FileMode.Create))
            using (var writer = new StreamWriter(stream, EncodingHelpers.UTF8))
            {
                var data = JsonConvert.SerializeObject(CommandHistory);
                _historyPosition = CommandHistory.Count;
                writer.Write(data);
            }
        }
    }
}
