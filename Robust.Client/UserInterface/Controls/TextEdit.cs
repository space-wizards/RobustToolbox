using System;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Collections;
using Robust.Shared.Console;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.UserInterface.Controls;

/// <summary>
/// A multi-line text editing control.
/// </summary>
public sealed class TextEdit : Control
{
    private readonly RenderBox _renderBox;

    private int _cursorPosition;
    private int _selectionStart;

    private ValueList<int> _lineBreaks;

    private bool _lineUpdateQueued;
    private Rope.Node _textRope = Rope.Leaf.Empty;

    public bool Editable { get; set; } = true;

    // Debug overlay stuff.
    internal bool DebugOverlay;
    private Vector2? _lastMousePos;

    private CursorBlink _blink;

    public TextEdit()
    {
        AddChild(_renderBox = new RenderBox(this));

        CanKeyboardFocus = true;
        KeyboardFocusOnClick = true;
        MouseFilter = MouseFilterMode.Stop;
    }

    public int CursorPosition
    {
        get => _cursorPosition;
        set
        {
            var clamped = MathHelper.Clamp(value, 0, TextLength);
            if (TextLength != 0 && TextLength != clamped && !Rope.TryGetRuneAt(TextRope, clamped, out _))
                throw new ArgumentException("Cannot set cursor inside surrogate pair.");

            _cursorPosition = clamped;
            _selectionStart = _cursorPosition;
        }
    }

    public int SelectionStart
    {
        get => _selectionStart;
        set
        {
            var clamped = MathHelper.Clamp(value, 0, TextLength);
            if (TextLength != 0 && TextLength != clamped && !Rope.TryGetRuneAt(TextRope, clamped, out _))
                throw new ArgumentException("Cannot set cursor inside surrogate pair.");

            _selectionStart = clamped;
        }
    }

    public Rope.Node TextRope
    {
        get => _textRope;
        set
        {
            _textRope = value;
            QueueLineBreakUpdate();
        }
    }

    public int SelectionLower => Math.Min(_selectionStart, _cursorPosition);
    public int SelectionUpper => Math.Max(_selectionStart, _cursorPosition);

    public int SelectionLength => Math.Abs(_selectionStart - _cursorPosition);

    // TODO: cache
    public int TextLength => (int)Rope.CalcTotalLength(TextRope);

    protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Handled)
            return;

        if (args.Function == EngineKeyFunctions.TextCursorRight)
        {
            if (_selectionStart != _cursorPosition)
            {
                _cursorPosition = _selectionStart = SelectionUpper;
            }
            else
            {
                ShiftCursorRight();

                _selectionStart = _cursorPosition;
            }

            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.TextCursorLeft)
        {
            if (_selectionStart != _cursorPosition)
            {
                _cursorPosition = _selectionStart = SelectionLower;
            }
            else
            {
                ShiftCursorLeft();

                _selectionStart = _cursorPosition;
            }

            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.TextBackspace)
        {
            if (Editable)
            {
                var changed = false;
                var oldText = _textRope;
                var cursor = _cursorPosition;
                var selectStart = _selectionStart;
                if (_selectionStart != _cursorPosition)
                {
                    TextRope = Rope.Delete(oldText, SelectionLower, SelectionLength);
                    _cursorPosition = SelectionLower;
                    changed = true;
                }
                else if (_cursorPosition != 0)
                {
                    var remPos = _cursorPosition - 1;
                    var remAmt = 1;
                    // If this is a low surrogate remove two chars to remove the whole pair.
                    if (char.IsLowSurrogate(Rope.Index(oldText, remPos)))
                    {
                        remPos -= 1;
                        remAmt = 2;
                    }

                    TextRope = Rope.Delete(oldText, remPos, remAmt);
                    _cursorPosition -= remAmt;
                    changed = true;
                }

                if (changed)
                {
                    _selectionStart = _cursorPosition;
                    // OnTextChanged?.Invoke(new LineEditEventArgs(this, _text));
                    // _updatePseudoClass();
                    // OnBackspace?.Invoke(new LineEditBackspaceEventArgs(oldText, _text, cursor, selectStart));
                }

                args.Handle();
            }
        }
        else if (args.Function == EngineKeyFunctions.TextDelete)
        {
            if (Editable)
            {
                var changed = false;
                if (_selectionStart != _cursorPosition)
                {
                    TextRope = Rope.Delete(TextRope, SelectionLower, SelectionLength);
                    _cursorPosition = SelectionLower;
                    changed = true;
                }
                else if (_cursorPosition < TextLength)
                {
                    var remAmt = 1;
                    if (char.IsHighSurrogate(Rope.Index(TextRope, _cursorPosition)))
                        remAmt = 2;

                    TextRope = Rope.Delete(TextRope, _cursorPosition, remAmt);
                    changed = true;
                }

                if (changed)
                {
                    _selectionStart = _cursorPosition;
                    // OnTextChanged?.Invoke(new LineEditEventArgs(this, _text));
                    // _updatePseudoClass();
                }

                args.Handle();
            }
        }
        else if (args.Function == EngineKeyFunctions.TextNewline)
        {
            InsertAtCursor("\n");

            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.TextSelectAll)
        {
            _cursorPosition = TextLength;
            _selectionStart = 0;
            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.UIClick || args.Function == EngineKeyFunctions.TextCursorSelect)
        {
            // _mouseSelectingText = true;
            // _lastMousePosition = args.RelativePosition.X;

            // Find closest cursor position under mouse.
            var index = GetIndexAtPos(args.RelativePosition);

            _cursorPosition = index;

            if (args.Function != EngineKeyFunctions.TextCursorSelect)
            {
                _selectionStart = index;
            }

            args.Handle();
        }

        // Reset this so the cursor is always visible immediately after a keybind is pressed.
        _blink.Reset();

        void ShiftCursorLeft()
        {
            if (_cursorPosition == 0)
                return;

            _cursorPosition -= 1;

            if (char.IsLowSurrogate(Rope.Index(TextRope, _cursorPosition)))
                _cursorPosition -= 1;
        }

        void ShiftCursorRight()
        {
            if (_cursorPosition == TextLength)
                return;

            _cursorPosition += 1;

            // Before you confuse yourself on "shouldn't this be high surrogate since shifting left checks low"
            // (Because yes, I did myself too a week after writing it)
            // char.IsLowSurrogate(_text[_cursorPosition]) means "is the cursor between a surrogate pair"
            // because we ALREADY moved.
            if (_cursorPosition != TextLength && char.IsLowSurrogate(Rope.Index(TextRope, _cursorPosition)))
                _cursorPosition += 1;
        }
    }

    protected internal override void TextEntered(GUITextEventArgs args)
    {
        base.TextEntered(args);

        if (!Editable)
            return;

        InsertAtCursor(args.AsRune.ToString());
        // OnTextTyped?.Invoke(args);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var size = base.ArrangeOverride(finalSize);

        UpdateLineBreaks((int)(size.X * UIScale));

        return size;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _blink.FrameUpdate(args);

        EnsureLineBreaksUpdated();
    }

    [Pure]
    private Font GetFont()
    {
        if (TryGetStyleProperty<Font>("font", out var font))
            return font;

        return UserInterfaceManager.ThemeDefaults.DefaultFont;
    }

    internal void QueueLineBreakUpdate()
    {
        _lineUpdateQueued = true;
    }

    private void EnsureLineBreaksUpdated()
    {
        if (_lineUpdateQueued)
            UpdateLineBreaks(PixelWidth);
    }


    public void InsertAtCursor(string text)
    {
        // Strip newlines.
        var lower = SelectionLower;
        var upper = SelectionUpper;

        var (left, mid) = Rope.Split(TextRope, lower);
        var (_, right) = Rope.Split(mid, upper - lower);

        TextRope = Rope.Concat(left, Rope.Concat(text, right));

        //if (!InternalSetText(newContents))
        //{
        //    return;
        //}

        _selectionStart = _cursorPosition = lower + text.Length;
        // OnTextChanged?.Invoke(new LineEditEventArgs(this, _text));
        // _updatePseudoClass();
    }

    private void UpdateLineBreaks(int pixelWidth)
    {
        _lineBreaks.Clear();

        var font = GetFont();
        var scale = UIScale;

        var wordWrap = new WordWrap(pixelWidth);
        int? breakLine;

        foreach (var rune in Rope.EnumerateRunes(TextRope))
        {
            wordWrap.NextRune(rune, out breakLine, out var breakNewLine, out var skip);
            CheckLineBreak(breakLine);
            CheckLineBreak(breakNewLine);
            if (skip)
                continue;

            // Uh just skip unknown characters I guess.
            if (!font.TryGetCharMetrics(rune, scale, out var metrics))
                continue;

            wordWrap.NextMetrics(metrics, out breakLine, out var abort);
            CheckLineBreak(breakLine);
            if (abort)
                return;
        }

        wordWrap.FinalizeText(out breakLine);
        CheckLineBreak(breakLine);

        void CheckLineBreak(int? line)
        {
            if (line is { } l)
            {
                _lineBreaks.Add(l);
                // Height += font.GetLineHeight(uiScale);
            }
        }

        _lineUpdateQueued = false;
    }

    private int GetIndexAtPos(Vector2 position)
    {
        EnsureLineBreaksUpdated();

        var contentBox = PixelSizeBox;

        var clickPos = position * UIScale;

        var _drawOffset = Vector2.Zero;

        var font = GetFont();

        var lineHeight = font.GetLineHeight(UIScale);

        (int, int?) FindVerticalLine()
        {
            // Step one: find the vertical line containing the mouse position.
            var lineIndex = (int)(clickPos.Y / lineHeight);

            if (lineIndex > _lineBreaks.Count)
            {
                // Below the last line, return the far end of the last line then.
                return (TextLength, null);
            }

            return (
                lineIndex == 0 ? 0 : _lineBreaks[lineIndex - 1],
                _lineBreaks.Count == lineIndex ? null : _lineBreaks[lineIndex]
            );
        }

        // textIdx = start index on the vertical line we're on.
        // breakIdx = where the next line starts.
        var (textIdx, breakIdx) = FindVerticalLine();

        var chrPosX = 0f;
        var lastChrPosX = 0f;
        var index = textIdx;
        foreach (var rune in Rope.EnumerateRunes(TextRope, textIdx))
        {
            if (index >= breakIdx)
            {
                break;
            }

            if (!font.TryGetCharMetrics(rune, UIScale, out var metrics))
            {
                index += rune.Utf16SequenceLength;
                continue;
            }

            if (chrPosX > clickPos.X)
            {
                break;
            }

            lastChrPosX = chrPosX;
            chrPosX += metrics.Advance;
            index += rune.Utf16SequenceLength;

            if (chrPosX > contentBox.Right)
            {
                break;
            }
        }

        // Distance between the right side of the glyph overlapping the mouse and the mouse.
        var distanceRight = chrPosX - clickPos.X;
        // Same but left side.
        var distanceLeft = clickPos.X - lastChrPosX;
        // If the mouse is closer to the left of the glyph we lower the index one, so we select before that glyph.
        if (index > 0 && distanceRight > distanceLeft)
        {
            index = (int)Rope.RuneShiftLeft(index, TextRope);
        }

        return index;
    }

    protected internal override void MouseExited()
    {
        base.MouseExited();

        _lastMousePos = null;
    }

    protected internal override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        _lastMousePos = args.RelativePosition;
    }

    private sealed class RenderBox : Control
    {
        private readonly TextEdit _master;
        private Vector2? _lastMousePos;

        public RenderBox(TextEdit master)
        {
            _master = master;

            RectClipContent = true;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            int? drawIndexDebug = null;
            if (_master.DebugOverlay && _master._lastMousePos is { } mouse)
            {
                drawIndexDebug = _master.GetIndexAtPos(mouse);
            }

            var drawBox = PixelSizeBox;
            var font = _master.GetFont();

            var scale = UIScale;
            var baseLine = new Vector2(0, font.GetAscent(scale));
            var height = font.GetLineHeight(scale);
            var descent = font.GetDescent(scale);

            var lineBreakIndex = 0;

            var count = 0;

            foreach (var rune in Rope.EnumerateRunes(_master.TextRope))
            {
                CheckDrawCursors();

                if (lineBreakIndex < _master._lineBreaks.Count
                    && _master._lineBreaks[lineBreakIndex] == count)
                {
                    baseLine = new Vector2(drawBox.Left, baseLine.Y + font.GetLineHeight(scale));
                    lineBreakIndex += 1;
                }

                baseLine.X += font.DrawChar(handle, rune, baseLine, scale, Color.White);

                count += rune.Utf16SequenceLength;
            }

            // Also draw cursor if it's at the very end.
            CheckDrawCursors();

            void CheckDrawCursors()
            {
                if (drawIndexDebug == count)
                {
                    handle.DrawRect(
                        new UIBox2(
                            baseLine.X,
                            baseLine.Y - height + descent,
                            baseLine.X + 1,
                            baseLine.Y + descent),
                        Color.Yellow);
                }

                if (_master._cursorPosition == count && _master.HasKeyboardFocus() && _master._blink.CurrentlyLit)
                {
                    handle.DrawRect(
                        new UIBox2(
                            baseLine.X,
                            baseLine.Y - height  + descent,
                            baseLine.X + 1,
                            baseLine.Y  + descent),
                        Color.White);
                }
            }
        }
    }
}

// To run these, you need a Command input keybinding for them.

// bind F12 Command textedit_ropeviz
internal sealed class TextEditRopeVizCommand : IConsoleCommand
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public string Command => "textedit_ropeviz";
    public string Description => "";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_ui.KeyboardFocused is TextEdit te)
        {
            new TextEditRopeViz(te).Show();
        }
    }
}

// bind F11 Command textedit_rebalance
internal sealed class TextEditRebalanceCommand : IConsoleCommand
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public string Command => "textedit_rebalance";
    public string Description => "";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_ui.KeyboardFocused is TextEdit te)
        {
            te.TextRope = Rope.Rebalance(te.TextRope);
        }
    }
}

// bind F10 Command textedit_debugoverlay
internal sealed class TextEditDebugOverlayCommand : IConsoleCommand
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public string Command => "textedit_debugoverlay";
    public string Description => "";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_ui.KeyboardFocused is TextEdit te)
        {
            te.DebugOverlay ^= true;
        }
    }
}

// bind F9 Command textedit_queuelinebreak
internal sealed class TextEditQueueLineBreakCommand : IConsoleCommand
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public string Command => "textedit_queuelinebreak";
    public string Description => "";
    public string Help => "";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_ui.KeyboardFocused is TextEdit te)
        {
            te.QueueLineBreakUpdate();
        }
    }
}
