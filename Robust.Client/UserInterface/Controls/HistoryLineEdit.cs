using System;
using System.Collections.Generic;
using System.Text;
using Robust.Shared.Input;

namespace Robust.Client.UserInterface.Controls
{
    public class HistoryLineEdit : LineEdit
    {
        private const int MaxHistorySize = 100;

        private readonly List<string> _history = new List<string>();
        private int _historyIndex = 0;
        private string _historyTemp;

        public HistoryLineEdit()
        {
            OnTextEntered += OnSubmit;
        }

        private void OnSubmit(LineEditEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(args.Text))
            {
                return;
            }

            if (_history.Count == 0 || _history[_history.Count - 1] != args.Text)
            {
                _history.Add(args.Text);
                if (_history.Count > MaxHistorySize)
                {
                    _history.RemoveAt(0);
                }
            }
            _historyIndex = _history.Count;
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
                if (_historyIndex <= 0)
                {
                    return;
                }

                if (_historyIndex == _history.Count)
                {
                    _historyTemp = Text;
                }

                _historyIndex--;
                Text = _history[_historyIndex];
                CursorPos = Text.Length;

                args.Handle();
            }
            else if (args.Function == EngineKeyFunctions.TextHistoryNext)
            {
                if (_historyIndex >= _history.Count)
                {
                    return;
                }

                _historyIndex++;

                if (_historyIndex == _history.Count)
                {
                    Text = _historyTemp;
                }
                else
                {
                    Text = _history[_historyIndex];
                }

                CursorPos = Text.Length;

                args.Handle();
            }
        }
    }
}
