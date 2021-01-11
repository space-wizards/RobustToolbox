using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.Input;

namespace Robust.Client.UserInterface.Controls
{
    public class HistoryLineEdit : LineEdit
    {
        private const int MaxHistorySize = 100;
        private string? _historyTemp;

        public List<string> History { get; } = new();
        public int HistoryIndex { get; set; } = 0;

        public event Action? OnHistoryChanged;

        public HistoryLineEdit()
        {
            OnTextEntered += OnSubmit;
        }

        public void ClearHistory()
        {
            History.Clear();
            HistoryIndex = 0;
        }

        private void OnSubmit(LineEditEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.Text))
            {
                return;
            }

            if (History.Count == 0 || History[History.Count - 1] != args.Text)
            {
                History.Add(args.Text);
                if (History.Count > MaxHistorySize)
                {
                    History.RemoveAt(0);
                }
            }
            HistoryIndex = History.Count;
            OnHistoryChanged?.Invoke();
        }

        protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (!this.HasKeyboardFocus())
            {
                return;
            }

            if (args.Function == EngineKeyFunctions.TextHistoryPrev)
            {
                if (HistoryIndex <= 0)
                {
                    return;
                }

                if (HistoryIndex == History.Count)
                {
                    _historyTemp = Text;
                }

                HistoryIndex--;
                Text = History[HistoryIndex];
                CursorPosition = Text.Length;

                args.Handle();
            }
            else if (args.Function == EngineKeyFunctions.TextHistoryNext)
            {
                if (HistoryIndex >= History.Count)
                {
                    return;
                }

                HistoryIndex++;

                if (HistoryIndex == History.Count)
                {
                    Text = _historyTemp!;
                }
                else
                {
                    Text = History[HistoryIndex];
                }

                CursorPosition = Text.Length;

                args.Handle();
            }
        }
    }
}
