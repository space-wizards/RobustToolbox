using System;
using System.Collections.Generic;
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

    public LineBreakBias CursorBias { get; set; }
    private int _cursorPosition;

    private float? _horizontalCursorPos;

    // TODO: Track cursor bias on selection start.
    private int _selectionStart;

    private ValueList<int> _lineBreaks;

    private bool _lineUpdateQueued;
    private Rope.Node _textRope = Rope.Leaf.Empty;

    public bool Editable { get; set; } = true;

    // Debug overlay stuff.
    internal bool DebugOverlay;
    private Vector2? _lastMousePos;

    private TextEditShared.CursorBlink _blink;

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

    private static readonly Dictionary<BoundKeyFunction, MoveType> MoveTypeMap = new()
    {
        // @formatter:off
        { EngineKeyFunctions.TextCursorLeft,            MoveType.Left      },
        { EngineKeyFunctions.TextCursorRight,           MoveType.Right     },
        { EngineKeyFunctions.TextCursorUp,              MoveType.Up        },
        { EngineKeyFunctions.TextCursorDown,            MoveType.Down      },
        { EngineKeyFunctions.TextCursorWordLeft,        MoveType.LeftWord  },
        { EngineKeyFunctions.TextCursorWordRight,       MoveType.RightWord },
        { EngineKeyFunctions.TextCursorBegin,           MoveType.Begin     },
        { EngineKeyFunctions.TextCursorEnd,             MoveType.End       },

        { EngineKeyFunctions.TextCursorSelectLeft,      MoveType.Left      | MoveType.SelectFlag },
        { EngineKeyFunctions.TextCursorSelectRight,     MoveType.Right     | MoveType.SelectFlag },
        { EngineKeyFunctions.TextCursorSelectUp,        MoveType.Up        | MoveType.SelectFlag },
        { EngineKeyFunctions.TextCursorSelectDown,      MoveType.Down      | MoveType.SelectFlag },
        { EngineKeyFunctions.TextCursorSelectWordLeft,  MoveType.LeftWord  | MoveType.SelectFlag },
        { EngineKeyFunctions.TextCursorSelectWordRight, MoveType.RightWord | MoveType.SelectFlag },
        { EngineKeyFunctions.TextCursorSelectBegin,     MoveType.Begin     | MoveType.SelectFlag },
        { EngineKeyFunctions.TextCursorSelectEnd,       MoveType.End       | MoveType.SelectFlag },
        // @formatter:on
    };

    protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Handled)
            return;

        if (MoveTypeMap.TryGetValue(args.Function, out var moveType))
        {
            var selectFlag = (moveType & MoveType.SelectFlag) != 0;
            var newPos = CalcKeyMove(moveType & MoveType.ActionMask, selectFlag, out var keepH);

            (_cursorPosition, CursorBias) = newPos;

            if (!selectFlag)
                _selectionStart = _cursorPosition;

            if (!keepH)
                InvalidateHorizontalCursorPos();

            args.Handle();
        }

        if (args.Function == EngineKeyFunctions.TextBackspace)
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

                InvalidateHorizontalCursorPos();

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

                InvalidateHorizontalCursorPos();

                args.Handle();
            }
        }
        else if (args.Function == EngineKeyFunctions.TextNewline)
        {
            InsertAtCursor("\n");

            InvalidateHorizontalCursorPos();

            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.TextSelectAll)
        {
            _cursorPosition = TextLength;
            _selectionStart = 0;

            InvalidateHorizontalCursorPos();

            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.UIClick || args.Function == EngineKeyFunctions.TextCursorSelect)
        {
            // _mouseSelectingText = true;
            // _lastMousePosition = args.RelativePosition.X;

            // Find closest cursor position under mouse.
            var index = GetIndexAtPos(args.RelativePosition);

            (_cursorPosition, CursorBias) = index;

            if (args.Function != EngineKeyFunctions.TextCursorSelect)
            {
                _selectionStart = _cursorPosition;
            }

            InvalidateHorizontalCursorPos();

            args.Handle();
        }

        // Reset this so the cursor is always visible immediately after a keybind is pressed.
        _blink.Reset();

        [Pure]
        int ShiftCursorLeft()
        {
            if (_cursorPosition == 0)
                return _cursorPosition;

            var pos = _cursorPosition - 1;

            if (char.IsLowSurrogate(Rope.Index(TextRope, pos)))
                pos -= 1;

            return pos;
        }

        [Pure]
        int ShiftCursorRight()
        {
            if (_cursorPosition == TextLength)
                return _cursorPosition;

            var pos = _cursorPosition + 1;

            // Before you confuse yourself on "shouldn't this be high surrogate since shifting left checks low"
            // (Because yes, I did myself too a week after writing it)
            // char.IsLowSurrogate(_text[_cursorPosition]) means "is the cursor between a surrogate pair"
            // because we ALREADY moved.
            if (pos != TextLength && char.IsLowSurrogate(Rope.Index(TextRope, pos)))
                pos += 1;

            return pos;
        }

        void CacheHorizontalCursorPos()
        {
            EnsureLineBreaksUpdated();

            if (_horizontalCursorPos != null)
                return;

            _horizontalCursorPos = GetHorizontalPositionAtIndex(_cursorPosition, CursorBias);
        }

        // NOTE: Much to my dismay, this isn't a pure function. It calls CacheHorizontalCursorPos().
        CursorPos CalcKeyMove(MoveType type, bool select, out bool keepHorizontalCursorPos)
        {
            keepHorizontalCursorPos = false;

            switch (type)
            {
                case MoveType.Left:
                {
                    if (_selectionStart != _cursorPosition && !select)
                    {
                        // TODO: Bias comes from selection start bias.
                        return new CursorPos(SelectionLower, LineBreakBias.Bottom);
                    }

                    var (_, lineStart, _) = GetLineForIndex(_cursorPosition, CursorBias);

                    if (CursorBias == LineBreakBias.Bottom && _cursorPosition == lineStart)
                    {
                        return new CursorPos(_cursorPosition, LineBreakBias.Top);
                    }

                    var newPos = ShiftCursorLeft();
                    var bias = Rope.Index(TextRope, newPos) == '\n'
                        ? LineBreakBias.Top
                        : LineBreakBias.Bottom;

                    return new CursorPos(newPos, bias);
                }
                case MoveType.Right:
                {
                    if (_selectionStart != _cursorPosition && !select)
                    {
                        // TODO: Bias comes from selection start bias.
                        return new CursorPos(SelectionUpper, LineBreakBias.Top);
                    }

                    var (_, _, lineEnd) = GetLineForIndex(_cursorPosition, CursorBias);
                    if (CursorBias == LineBreakBias.Top
                        && _cursorPosition == lineEnd
                        && _cursorPosition != TextLength
                        && Rope.Index(TextRope, _cursorPosition) != '\n')
                    {
                        return new CursorPos(_cursorPosition, LineBreakBias.Bottom);
                    }

                    return new CursorPos(ShiftCursorRight(), LineBreakBias.Top);
                }
                case MoveType.LeftWord:
                {
                    var runes = Rope.EnumerateRunesReverse(TextRope, _cursorPosition);
                    var pos = _cursorPosition + TextEditShared.PrevWordPosition(runes.GetEnumerator());

                    return new CursorPos(pos, LineBreakBias.Bottom);
                }
                case MoveType.RightWord:
                {
                    var runes = Rope.EnumerateRunes(TextRope, _cursorPosition);
                    var pos = _cursorPosition + TextEditShared.NextWordPosition(runes.GetEnumerator());

                    return new CursorPos(pos, LineBreakBias.Bottom);
                }
                case MoveType.Up:
                {
                    CacheHorizontalCursorPos();

                    // TODO: From selection lower, not cursor pos.
                    var (line, _, _) = GetLineForIndex(_cursorPosition, CursorBias);

                    if (line == 0)
                    {
                        // We're on the top line already, move to the start of it instead.
                        return new CursorPos(0, LineBreakBias.Top);
                    }

                    keepHorizontalCursorPos = true;

                    return GetIndexAtHorizontalPos(line - 1, _horizontalCursorPos!.Value);
                }
                case MoveType.Down:
                {
                    CacheHorizontalCursorPos();

                    var (line, _, _) = GetLineForIndex(_cursorPosition, CursorBias);

                    // TODO: Isn't this off-by-one.
                    if (line == _lineBreaks.Count - 1)
                    {
                        // On the last line already, move to the end of it.
                        return new CursorPos(_cursorPosition, LineBreakBias.Top);
                    }

                    return GetIndexAtHorizontalPos(line + 1, _horizontalCursorPos!.Value);
                }
                case MoveType.Begin:
                {
                    var (_, lineStart, _) = GetLineForIndex(_cursorPosition, CursorBias);
                    if (Rope.Index(TextRope, lineStart) == '\n')
                        lineStart += 1;

                    return new CursorPos(lineStart, LineBreakBias.Bottom);
                }
                case MoveType.End:
                {
                    var (_, _, lineEnd) = GetLineForIndex(_cursorPosition, CursorBias);
                    return new CursorPos(lineEnd, LineBreakBias.Top);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }

    private void InvalidateHorizontalCursorPos()
    {
        _horizontalCursorPos = null;
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
        InvalidateHorizontalCursorPos();

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

    private CursorPos GetIndexAtPos(Vector2 position)
    {
        EnsureLineBreaksUpdated();

        var contentBox = PixelSizeBox;

        var clickPos = position * UIScale;

        var _drawOffset = Vector2.Zero;

        var font = GetFont();

        var lineHeight = font.GetLineHeight(UIScale);
        var lineIndex = (int)(clickPos.Y / lineHeight);

        return GetIndexAtHorizontalPos(lineIndex, position.X);
    }

    private CursorPos GetIndexAtHorizontalPos(int line, float horizontalPos)
    {
        var contentBox = PixelSizeBox;
        var font = GetFont();
        horizontalPos *= UIScale;

        (int, int) FindVerticalLine()
        {
            // Step one: find the vertical line containing the mouse position.

            if (line > _lineBreaks.Count)
            {
                // Below the last line, return the far end of the last line then.
                return (TextLength, TextLength);
            }

            return (
                line == 0 ? 0 : _lineBreaks[line - 1],
                _lineBreaks.Count == line ? TextLength : _lineBreaks[line]
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

            if (chrPosX > horizontalPos)
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
        var distanceRight = chrPosX - horizontalPos;
        // Same but left side.
        var distanceLeft = horizontalPos - lastChrPosX;
        // If the mouse is closer to the left of the glyph we lower the index one, so we select before that glyph.
        if (index > 0 && distanceRight > distanceLeft)
        {
            index = (int)Rope.RuneShiftLeft(index, TextRope);
        }

        return new CursorPos(index, index == textIdx ? LineBreakBias.Bottom : LineBreakBias.Top);
    }

    private float GetHorizontalPositionAtIndex(int index, LineBreakBias bias)
    {
        EnsureLineBreaksUpdated();

        var hPos = 0;
        var font = GetFont();

        var (_, lineStart, _) = GetLineForIndex(index, bias);
        using var runeEnumerator = Rope.EnumerateRunes(TextRope, lineStart).GetEnumerator();

        var i = lineStart;
        while (true)
        {
            if (i >= index)
                break;

            if (!runeEnumerator.MoveNext())
                break;

            var rune = runeEnumerator.Current;
            if (font.TryGetCharMetrics(rune, UIScale, out var metrics))
                hPos += metrics.Advance;

            i += rune.Utf16SequenceLength;
        }

        return hPos;
    }

    private (int lineIdx, int lineStart, int lineEnd) GetLineForIndex(int index, LineBreakBias bias)
    {
        DebugTools.Assert(index >= 0);

        EnsureLineBreaksUpdated();

        if (_lineBreaks.Count == 0)
            return (0, 0, TextLength);

        int i;
        for (i = 0; i < _lineBreaks.Count; i++)
        {
            var lineIdx = _lineBreaks[i];
            if (bias == LineBreakBias.Bottom ? (lineIdx > index) : (lineIdx >= index))
            {
                if (i == 0)
                {
                    // First line
                    return (0, 0, lineIdx);
                }

                return (i, _lineBreaks[i - 1], lineIdx);
            }
        }

        // Position is on last line.
        return (_lineBreaks.Count, _lineBreaks[^1], TextLength);
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
        private static readonly (Vector2, Vector2)[] ArrowUp =
        {
            ((8, 14), (8, 2)),
            ((4, 7), (8, 2)),
            ((12, 7), (8, 2)),
        };

        private static readonly (Vector2, Vector2)[] ArrowDown =
        {
            ((8, 14), (8, 2)),
            ((4, 9), (8, 14)),
            ((12, 9), (8, 14)),
        };

        private static readonly Vector2 ArrowSize = (16, 16);

        private readonly TextEdit _master;
        private Vector2? _lastMousePos;

        public RenderBox(TextEdit master)
        {
            _master = master;

            RectClipContent = true;
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            CursorPos? drawIndexDebug = null;
            if (_master.DebugOverlay && _master._lastMousePos is { } mouse)
            {
                drawIndexDebug = _master.GetIndexAtPos(mouse);
            }

            var drawBox = PixelSizeBox;
            var font = _master.GetFont();

            if (_master.DebugOverlay && _master._horizontalCursorPos is { } hPos)
            {
                handle.DrawLine(
                    (hPos + drawBox.Left, drawBox.Top),
                    (hPos + drawBox.Left, drawBox.Bottom),
                    Color.Purple);
            }

            var scale = UIScale;
            var baseLine = new Vector2(0, font.GetAscent(scale));
            var height = font.GetLineHeight(scale);
            var descent = font.GetDescent(scale);

            var lineBreakIndex = 0;

            var count = 0;

            foreach (var rune in Rope.EnumerateRunes(_master.TextRope))
            {
                CheckDrawCursors(LineBreakBias.Top);

                if (lineBreakIndex < _master._lineBreaks.Count
                    && _master._lineBreaks[lineBreakIndex] == count)
                {
                    baseLine = new Vector2(drawBox.Left, baseLine.Y + font.GetLineHeight(scale));
                    lineBreakIndex += 1;
                }

                CheckDrawCursors(LineBreakBias.Bottom);

                var color = Color.White;
                if (count >= _master.SelectionLower && count < _master.SelectionUpper)
                    color = Color.Red;

                baseLine.X += font.DrawChar(handle, rune, baseLine, scale, color);

                count += rune.Utf16SequenceLength;
            }

            // Also draw cursor if it's at the very end.
            CheckDrawCursors(LineBreakBias.Bottom);
            CheckDrawCursors(LineBreakBias.Top);

            // Draw cursor bias
            if (_master.DebugOverlay)
            {
                var arrow = _master.CursorBias == LineBreakBias.Bottom ? ArrowDown : ArrowUp;
                foreach (var (to, from) in arrow)
                {
                    var offset = new Vector2(0, drawBox.Bottom - ArrowSize.Y);
                    handle.DrawLine(to + offset, from + offset, Color.Green);
                }
            }

            void CheckDrawCursors(LineBreakBias bias)
            {
                if (drawIndexDebug != null
                    && drawIndexDebug.Value.Index == count
                    && drawIndexDebug.Value.Bias == bias)
                {
                    handle.DrawRect(
                        new UIBox2(
                            baseLine.X,
                            baseLine.Y - height + descent,
                            baseLine.X + 1,
                            baseLine.Y + descent),
                        Color.Yellow);
                }

                if (_master._cursorPosition == count
                    && _master.HasKeyboardFocus()
                    && _master._blink.CurrentlyLit
                    && _master.CursorBias == bias)
                {
                    handle.DrawRect(
                        new UIBox2(
                            baseLine.X,
                            baseLine.Y - height + descent,
                            baseLine.X + 1,
                            baseLine.Y + descent),
                        Color.White);
                }
            }
        }
    }

    public enum LineBreakBias
    {
        Top,
        Bottom
    }

    public record struct CursorPos(int Index, LineBreakBias Bias);

    [Flags]
    private enum MoveType
    {
        // @formatter:off
        Left       = 1 << 0,
        Right      = 1 << 1,
        LeftWord   = 1 << 2,
        RightWord  = 1 << 3,
        Up         = 1 << 4,
        Down       = 1 << 5,
        Begin      = 1 << 6,
        End        = 1 << 7,

        ActionMask         = (1 << 16) - 1,
        SelectFlag         =  1 << 16,
        // @formatter:on
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
