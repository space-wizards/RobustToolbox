using System;
using System.Collections.Generic;
using Robust.Shared.Input;

namespace Robust.Client.UserInterface.Controls
{
    [Virtual]
    public class HistoryLineEdit : LineEdit
    {
        private const int MaxHistorySize = 100;
        private string? _historyTemp;

        public List<string> History { get; } = new();
        public int HistoryIndex { get; set; } = 0;

        public event Action? OnHistoryChanged;

        /// <summary>
        ///     If true, this will cause <see cref="LineEdit.OnTextChanged"/> to be invoked when the history
        ///     selection changes.
        /// </summary>
        public bool InvokeOnHistorySelect = true;

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

            if (!HasKeyboardFocus())
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
                SetText(History[HistoryIndex], InvokeOnHistorySelect);
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
                    SetText(_historyTemp ?? string.Empty, InvokeOnHistorySelect);
                }
                else
                {
                    SetText(History[HistoryIndex], InvokeOnHistorySelect);
                }

                CursorPosition = Text.Length;

                args.Handle();
            }
        }
    }
}
