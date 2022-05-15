using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.Console;
using Robust.Shared.Input;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.CustomControls;

public sealed partial class DebugConsole
{
    private readonly DebugConsoleCompletion _compPopup;

    private CompletionResult? _compCurResult;

    private int _compParamCount;

    // The filtered set of completions currently shown to the user.
    private List<string>? _compFiltered;
    private int _compSelected;
    private int _compVerticalOffset;

    // Used for sequencing to nicely handle out-of-order completion responses.
    private int _compSeqSend;
    private int _compSeqRecv;


    private CancellationTokenSource _compCancel = new();

    private void InitCompletions()
    {
        CommandBar.OnTextTyped += CommandBarOnOnTextTyped;
        CommandBar.OnFocusExit += CommandBarOnOnFocusExit;
        CommandBar.OnBackspace += CommandBarOnOnBackspace;
    }

    private void CommandBarOnOnFocusExit(LineEdit.LineEditEventArgs obj)
    {
        AbortActiveCompletions();
    }

    private void CommandBarOnOnTextTyped(GUITextEventArgs obj)
    {
        if (_compCurResult == null)
        {
            TypeUpdateCompletions(true);
            return;
        }

        // var filtered = FilterCompletions(_completionsCurrentResult.Options, typingArg);
        // if (filtered.Count == 1 && filtered[0] == typingArg)
        TypeUpdateCompletions(true);
        // else
        //     TypeUpdateCompletions(false);
    }

    private void CommandBarOnOnBackspace(LineEdit.LineEditBackspaceEventArgs eventArgs)
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

        if (_compCurResult == null)
        {
            TypeUpdateCompletions(true);
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
        var (args, _) = CalcTypingArgs();

        if (args.Count != _compParamCount)
        {
            _compParamCount = args.Count;
            AbortActiveCompletions();
        }

        if (fullUpdate)
        {
            var seq = ++_compSeqSend;
            var task = _consoleHost.GetCompletions(args, _compCancel.Token);

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

        var (_, curTyping) = CalcTypingArgs();

        var curSelected = _compFiltered?.Count > 0 ? _compFiltered[_compSelected] : null;
        _compFiltered = FilterCompletions(_compCurResult.Options, curTyping);
        if (curSelected == null)
        {
            _compSelected = 0;
        }
        else
        {
            var foundIdx = _compFiltered.IndexOf(curSelected);
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

        var (_, curTyping) = CalcTypingArgs();

        var offset = CommandBar.GetOffsetAtIndex(CommandBar.CursorPosition - curTyping.Length);
        // Logger.Debug($"Offset: {offset}");

        _compPopup.Close();

        _compPopup.Contents.RemoveAllChildren();

        if (_compCurResult!.Hint != null)
        {
            var hint = _compCurResult.Hint;
            _compPopup.Contents.AddChild(new Label
            {
                Text = hint,
                FontColorOverride = Color.DarkGray
            });
        }

        // Fill out list completions.
        var maxCount = _cfg.GetCVar(CVars.ConCompletionCount);
        var c = 0;
        for (var i = _compVerticalOffset; i < _compFiltered.Count && c < maxCount; i++, c++)
        {
            _compPopup.Contents.AddChild(new Label
            {
                Text = _compFiltered[i],
                FontColorOverride = i == _compSelected ? Color.White : Color.DarkGray
            });
        }

        if (_compPopup.Contents.ChildCount != 0)
        {
            _compPopup.Open(
                UIBox2.FromDimensions(
                    offset - _compPopup.Contents.Margin.Left, CommandBar.GlobalPosition.Y + CommandBar.Height + 2,
                    5, 5));
        }
    }

    private (List<string> args, string curTyping) CalcTypingArgs()
    {
        var cursor = CommandBar.CursorPosition;
        // Don't consider text after the cursor.
        var text = CommandBar.Text[..cursor];

        var args = new List<string>();
        CommandParsing.ParseArguments(text, args);

        if (args.Count == 0 || text[^1] == ' ')
            args.Add("");

        return (args, args[^1]);
    }

    private List<string> FilterCompletions(IEnumerable<string> completions, string curTyping)
    {
        return completions
            .Where(c => c.StartsWith(curTyping, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
    }

    private void CompletionKeyDown(GUIBoundKeyEventArgs args)
    {
        if (args.Function == EngineKeyFunctions.TextTabComplete)
        {
            if (_compFiltered != null && _compSelected < _compFiltered.Count)
            {
                // Figure out typing word so we know how much to replace.
                var completion = _compFiltered[_compSelected];
                var (_, typing) = CalcTypingArgs();

                // Replace the full word from the start.
                // This means that letter casing will match the completion suggestion.
                CommandBar.CursorPosition = CommandBar.Text.Length;
                CommandBar.SelectionStart = CommandBar.Text.Length - typing.Length;
                CommandBar.InsertAtCursor(completion + " ");

                TypeUpdateCompletions(true);

                args.Handle();
            }

            return;
        }

        if (args.Function == EngineKeyFunctions.TextCompleteNext)
        {
            if (_compFiltered == null)
                return;

            args.Handle();
            var len = _compFiltered.Count;
            var pos = (_compSelected + 1) % len;
            _compSelected = pos;

            FixScrollMargins();

            UpdateCompletionsPopup();

            return;
        }

        if (args.Function == EngineKeyFunctions.TextCompletePrev)
        {
            if (_compFiltered == null)
                return;

            args.Handle();
            var len = _compFiltered.Count;
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
        var showCount = Math.Min(maxCount, _compFiltered.Count);
        var margin = _cfg.GetCVar(CVars.ConCompletionMargin);

        var posBottom = showCount + _compVerticalOffset - margin;
        if (_compSelected >= posBottom)
            _compVerticalOffset = Math.Min(_compFiltered.Count - showCount, _compSelected + 1 + margin - showCount);

        if (_compSelected < _compVerticalOffset + margin)
            _compVerticalOffset = Math.Max(0, _compSelected - margin);
    }

    private void CompletionCommandEntered()
    {
        AbortActiveCompletions();
    }
}
