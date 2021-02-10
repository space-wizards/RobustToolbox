using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Robust.Client.Console;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.ContentPack;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls
{
    public interface IDebugConsoleView
    {
        /// <summary>
        /// Write a line with a specific color to the console window.
        /// </summary>
        void AddLine(string text, Color color);

        void AddLine(string text);

        void AddFormattedLine(FormattedMessage message);

        void Clear();
    }

    // Quick note on how thread safety works in here:
    // Messages from other threads are not actually immediately drawn. They're stored in a queue.
    // Every frame OR the next time a message on the main thread comes in, this queue is drained.
    // This keeps thread safety while still making it so messages are ordered how they come in.
    // And also if Update() stops firing due to an exception loop the console will still work.
    // (At least from the main thread, which is what's throwing the exceptions..)
    public class DebugConsole : Control, IDebugConsoleView
    {
        private readonly IClientConsoleHost _consoleHost;
        private readonly IResourceManager _resourceManager;

        private static readonly ResourcePath HistoryPath = new("/debug_console_history.json");

        private readonly HistoryLineEdit CommandBar;
        private readonly OutputPanel Output;
        private readonly Control MainControl;

        private readonly ConcurrentQueue<FormattedMessage> _messageQueue = new();

        private bool _targetVisible;

        private bool commandChanged = true;
        private readonly List<string> searchResults;
        private int searchIndex = 0;

        public DebugConsole(IClientConsoleHost consoleHost, IResourceManager resMan)
        {
            _consoleHost = consoleHost;
            _resourceManager = resMan;

            Visible = false;

            var styleBox = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#25252add"),
            };
            styleBox.SetContentMarginOverride(StyleBox.Margin.All, 3);

            AddChild(new LayoutContainer
            {
                Children =
                {
                    (MainControl = new VBoxContainer
                    {
                        SeparationOverride = 0,
                        Children =
                        {
                            (Output = new OutputPanel
                            {
                                SizeFlagsVertical = SizeFlags.FillExpand,
                                StyleBoxOverride = styleBox
                            }),
                            (CommandBar = new HistoryLineEdit {PlaceHolder = "Command Here"})
                        }
                    })
                }
            });

            LayoutContainer.SetAnchorPreset(MainControl, LayoutContainer.LayoutPreset.TopWide);
            LayoutContainer.SetAnchorBottom(MainControl, 0.35f);

            CommandBar.OnTextChanged += OnCommandChanged;
            CommandBar.OnKeyBindDown += CommandBarOnOnKeyBindDown;
            CommandBar.OnTextEntered += CommandEntered;
            CommandBar.OnHistoryChanged += OnHistoryChanged;

            _consoleHost.AddString += (_, args) => AddLine(args.Text, DetermineColor(args.Local, args.Error));
            _consoleHost.AddFormatted += (_, args) => AddFormattedLine(args.Message);
            _consoleHost.ClearText += (_, args) => Clear();

            _loadHistoryFromDisk();

            searchResults = new List<string>();
        }

        private Color DetermineColor(bool local, bool error)
        {
            return Color.White;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            _flushQueue();

            if (!Visible)
            {
                return;
            }

            var targetLocation = _targetVisible ? 0 : -MainControl.Height;
            var (posX, posY) = MainControl.Position;

            if (Math.Abs(targetLocation - posY) <= 1)
            {
                if (!_targetVisible)
                {
                    Visible = false;
                }

                posY = targetLocation;
            }
            else
            {
                posY = MathHelper.Lerp(posY, targetLocation, args.DeltaSeconds * 20);
            }

            LayoutContainer.SetPosition(MainControl, (posX, posY));
        }

        public void Toggle()
        {
            _targetVisible = !_targetVisible;
            if (_targetVisible)
            {
                Visible = true;
                CommandBar.IgnoreNext = true;
                CommandBar.GrabKeyboardFocus();
            }
            else
            {
                CommandBar.ReleaseKeyboardFocus();
            }
        }

        private void CommandEntered(LineEdit.LineEditEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.Text))
            {
                _consoleHost.ExecuteCommand(args.Text);
                CommandBar.Clear();
            }

            commandChanged = true;
        }

        private void OnHistoryChanged()
        {
            _flushHistoryToDisk();
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
            _messageQueue.Enqueue(message);
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
            while (_messageQueue.TryDequeue(out var message))
            {
                _addFormattedLineInternal(message);
            }
        }

        private void CommandBarOnOnKeyBindDown(GUIBoundKeyEventArgs args)
        {
            if (args.Function == EngineKeyFunctions.ShowDebugConsole)
            {
                Toggle();
                args.Handle();
            }
            else if (args.Function == EngineKeyFunctions.TextReleaseFocus)
            {
                Toggle();
                args.Handle();
            }
            else if (args.Function == EngineKeyFunctions.TextScrollToBottom)
            {
                Output.ScrollToBottom();
                args.Handle();
            }
            else if (args.Function == EngineKeyFunctions.GuiTabNavigateNext)
            {
                NextCommand();
                args.Handle();
            }
            else if (args.Function == EngineKeyFunctions.GuiTabNavigatePrev)
            {
                PrevCommand();
                args.Handle();
            }
        }

        private void SetInput(string cmd)
        {
            CommandBar.Text = cmd;
            CommandBar.CursorPosition = cmd.Length;
        }

        private void FindCommands()
        {
            searchResults.Clear();
            searchIndex = 0;
            commandChanged = false;
            foreach (var cmd in _consoleHost.RegisteredCommands)
            {
                if (cmd.Key.StartsWith(CommandBar.Text))
                {
                    searchResults.Add(cmd.Key);
                }
            }
        }

        private void NextCommand()
        {
            if (!commandChanged)
            {
                if (searchResults.Count == 0)
                    return;

                searchIndex = (searchIndex + 1) % searchResults.Count;
                SetInput(searchResults[searchIndex]);
                return;
            }

            FindCommands();
            if (searchResults.Count == 0)
                return;

            SetInput(searchResults[0]);
        }

        private void PrevCommand()
        {
            if (!commandChanged)
            {
                if (searchResults.Count == 0)
                    return;

                searchIndex = MathHelper.Mod(searchIndex - 1, searchResults.Count);
                SetInput(searchResults[searchIndex]);
                return;
            }

            FindCommands();
            if (searchResults.Count == 0)
                return;

            SetInput(searchResults[^1]);
        }

        private void OnCommandChanged(LineEdit.LineEditEventArgs args)
        {
            commandChanged = true;
        }

        private async void _loadHistoryFromDisk()
        {
            CommandBar.ClearHistory();
            Stream stream;
            try
            {
                stream = _resourceManager.UserData.OpenRead(HistoryPath);
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
                    var data = JsonConvert.DeserializeObject<List<string>>(await reader.ReadToEndAsync());
                    CommandBar.ClearHistory();
                    CommandBar.History.AddRange(data);
                    CommandBar.HistoryIndex = CommandBar.History.Count;
                }
            }
            finally
            {
                stream?.Dispose();
            }
        }

        private void _flushHistoryToDisk()
        {
            using (var stream = _resourceManager.UserData.Create(HistoryPath))
            using (var writer = new StreamWriter(stream, EncodingHelpers.UTF8))
            {
                var data = JsonConvert.SerializeObject(CommandBar.History);
                CommandBar.HistoryIndex = CommandBar.History.Count;
                writer.Write(data);
            }
        }
    }
}
