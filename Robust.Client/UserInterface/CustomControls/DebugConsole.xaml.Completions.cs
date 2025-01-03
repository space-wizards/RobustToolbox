using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Collections;
using Robust.Shared.Console;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls;

public sealed partial class DebugConsole
{
    private readonly DebugConsoleCompletion _compPopup;

    // Last valid completion result we got.
    private CompletionResult? _compCurResult;

    // The parameter count for the above completion result.
    // Used to immediately invalidate it if the amount changes.
    private int _compParamCount;

    // The filtered set of completions currently shown to the user.
    private CompletionOption[]? _compFiltered;

    // Which completion is currently selected, index into _compFiltered.
    private int _compSelected;

    // Vertical scroll offset of the completion list.
    private int _compVerticalOffset;

    // Used for sequencing to nicely handle out-of-order completion responses.
    private int _compSeqSend;
    private int _compSeqRecv;


    private CancellationTokenSource _compCancel = new();

    private void InitCompletions()
    {
        CommandBar.OnTextTyped += CommandBarOnOnTextTyped;
        CommandBar.OnFocusExit += CommandBarOnOnFocusExit;
        CommandBar.OnTextRemoved += CommandBarOnTextRemoved;
    }

    private void CommandBarOnOnFocusExit(LineEdit.LineEditEventArgs obj)
    {
        AbortActiveCompletions();
    }

    private void CommandBarOnOnTextTyped(GUITextEnteredEventArgs obj)
    {
        TypeUpdateCompletions(true);
    }

    private void CommandBarOnTextRemoved(LineEdit.LineEditTextRemovedEventArgs eventArgs)
    {
        if (eventArgs.OldCursorPosition == 0 || eventArgs.OldSelectionStart != eventArgs.OldCursorPosition)
        {
            AbortActiveCompletions();
            return;
        }

        if (CommandBar.CursorPosition == 0)
        {
            // Don't do completions if you have nothing typed.
            AbortActiveCompletions();
            return;
        }

        TypeUpdateCompletions(true);
    }


    private void AbortActiveCompletions()
    {
        _compCurResult = null;
        _compFiltered = null;
        _compSelected = 0;
        _compVerticalOffset = 0;
        _compPopup.Close();

        _compCancel.Cancel();
        _compCancel.Dispose();
        _compCancel = new CancellationTokenSource();
    }

    private async void TypeUpdateCompletions(bool fullUpdate)
    {
        var (args, _, _, str) = CalcTypingArgs();

        if (args.Count != _compParamCount)
        {
            _compParamCount = args.Count;
            AbortActiveCompletions();
        }

        if (fullUpdate)
        {
            var seq = ++_compSeqSend;
            var task = _consoleHost.GetCompletions(args, str, _compCancel.Token);

            if (!task.IsCompleted)
            {
                // If we don't immediately get a result from the console (e.g. server command),
                // we update the filtered immediately before asynchronously waiting on it.
                UpdateFilteredCompletions();

                // This means we only update completions once when running synchronously.
            }

            CompletionResult result;
            try
            {
                result = await task;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (seq < _compSeqRecv)
            {
                // Newer result already came before us.
                return;
            }

            _compSeqRecv = seq;
            _compCurResult = result;
        }

        UpdateFilteredCompletions();
    }

    private void UpdateFilteredCompletions()
    {
        if (_compCurResult == null)
            return;

        var (_, curTyping, _, _) = CalcTypingArgs();

        var curSelected = _compFiltered?.Length > 0 ? _compFiltered[_compSelected] : default;
        _compFiltered = FilterCompletions(_compCurResult.Options, curTyping);
        if (curSelected == default)
        {
            _compSelected = 0;
        }
        else
        {
            var foundIdx = Array.IndexOf(_compFiltered, curSelected);
            _compSelected = foundIdx > 0 ? foundIdx : 0;
        }

        FixScrollMargins();

        // Logger.Debug($"Filtered completions: {string.Join(", ", _compFiltered)}");

        UpdateCompletionsPopup();
    }

    private void UpdateCompletionsPopup()
    {
        if (_compFiltered == null)
            return;

        DebugTools.AssertNotNull(_compCurResult);

        var (_, _, endRange, _) = CalcTypingArgs();

        var offset = CommandBar.GetOffsetAtIndex(endRange.start);
        // Logger.Debug($"Offset: {offset}");

        _compPopup.Close();

        _compPopup.Contents.RemoveAllChildren();

        if (_compCurResult!.Hint != null)
        {
            var hint = _compCurResult.Hint;
            _compPopup.Contents.AddChild(new Label
            {
                Text = hint,
                FontColorOverride = Color.Gray
            });
        }

        // Fill out list completions.
        var maxCount = _cfg.GetCVar(CVars.ConCompletionCount);
        var c = 0;
        for (var i = _compVerticalOffset; i < _compFiltered.Length && c < maxCount; i++, c++)
        {
            var (value, hint, _) = _compFiltered[i];

            var labelValue = new Label
            {
                Text = value,
                FontColorOverride = i == _compSelected ? Color.White : Color.DarkGray
            };

            if (hint != null)
            {
                _compPopup.Contents.AddChild(new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    Children =
                    {
                        labelValue,
                        new Label
                        {
                            Text = $" - {hint}",
                            FontColorOverride = Color.Gray
                        }
                    }
                });
            }
            else
            {
                _compPopup.Contents.AddChild(labelValue);
            }
        }

        if (_compPopup.Contents.ChildCount != 0)
        {
            _compPopup.Open(
                UIBox2.FromDimensions(
                    offset - _compPopup.Contents.Margin.Left, CommandBar.GlobalPosition.Y + CommandBar.Height + 2,
                    5, 5));
        }
    }

    private (List<string> args, string curTyping, (int start, int end) lastRange, string argStr) CalcTypingArgs()
    {
        var cursor = CommandBar.CursorPosition;
        // Don't consider text after the cursor.
        var text = CommandBar.Text.AsSpan(0, cursor);

        var args = new List<string>();
        var ranges = new ValueList<(int start, int end)>();
        CommandParsing.ParseArguments(text, args, ref ranges);

        if (args.Count == 0 || ranges[^1].end != text.Length)
            args.Add("");

        (int, int) lastRange;
        if (ranges.Count == 0)
            lastRange = default;
        else if (ranges.Count == args.Count)
            lastRange = ranges[^1];
        else
            lastRange = (cursor, cursor);

        return (args, args[^1], lastRange, text.ToString());
    }

    private CompletionOption[] FilterCompletions(IEnumerable<CompletionOption> completions, string curTyping)
    {
        return completions
            .Where(c => c.Value.StartsWith(curTyping, StringComparison.CurrentCultureIgnoreCase))
            .ToArray();
    }

    private void CompletionKeyDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function == EngineKeyFunctions.TextTabComplete)
        {
            if (_compFiltered != null && _compSelected < _compFiltered.Length)
            {
                // Figure out typing word so we know how much to replace.
                var (completion, _, completionFlags) = _compFiltered[_compSelected];
                var (_, _, lastRange, _) = CalcTypingArgs();

                // Replace the full word from the start.
                // This means that letter casing will match the completion suggestion.
                CommandBar.CursorPosition = lastRange.end;
                CommandBar.SelectionStart = lastRange.start;

                var insertValue = (completionFlags & CompletionOptionFlags.NoEscape) == 0
                    ? CommandParsing.Escape(completion)
                    : completion;

                // If the replacement contains a space, we must quote it to treat it as a single argument.
                var mustQuote = (completionFlags & CompletionOptionFlags.NoQuote) == 0 && insertValue.Contains(' ');

                if ((completionFlags & CompletionOptionFlags.PartialCompletion) == 0)
                {
                    if (mustQuote)
                        insertValue = $"\"{insertValue}\"";

                    insertValue += " ";
                }
                else if (mustQuote)
                {
                    // If it's a partial completion, only quote the start.
                    insertValue = '"' + insertValue;
                }

                CommandBar.InsertAtCursor(insertValue);

                TypeUpdateCompletions(true);

                args.Handle();
            }

            return;
        }

        if (args.Function == EngineKeyFunctions.TextCompleteNext)
        {
            if (_compFiltered == null || _compFiltered.Length == 0)
                return;

            args.Handle();
            var len = _compFiltered.Length;
            var pos = (_compSelected + 1) % len;
            _compSelected = pos;

            FixScrollMargins();

            UpdateCompletionsPopup();

            return;
        }

        if (args.Function == EngineKeyFunctions.TextCompletePrev)
        {
            if (_compFiltered == null || _compFiltered.Length == 0)
                return;

            args.Handle();
            var len = _compFiltered.Length;
            var pos = MathHelper.Mod(_compSelected - 1, len);
            _compSelected = pos;

            FixScrollMargins();

            UpdateCompletionsPopup();

            return;
        }
    }

    private void FixScrollMargins()
    {
        if (_compFiltered == null)
            return;

        var maxCount = _cfg.GetCVar(CVars.ConCompletionCount);
        var showCount = Math.Min(maxCount, _compFiltered.Length);
        var margin = _cfg.GetCVar(CVars.ConCompletionMargin);

        var posBottom = showCount + _compVerticalOffset - margin;
        if (_compSelected >= posBottom)
            _compVerticalOffset = Math.Min(_compFiltered.Length - showCount, _compSelected + 1 + margin - showCount);

        if (_compSelected < _compVerticalOffset + margin)
            _compVerticalOffset = Math.Max(0, _compSelected - margin);
    }

    private void CompletionCommandEntered()
    {
        AbortActiveCompletions();
    }
}
