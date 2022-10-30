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
    [Dependency] private readonly IClipboardManager _clipboard = null!;

    public const string StylePropertyStyleBox = "stylebox";
    public const string StylePropertyCursorColor = "cursor-color";
    public const string StylePropertySelectionColor = "selection-color";
    public const string StyleClassLineEditNotEditable = "notEditable";
    public const string StylePseudoClassPlaceholder = "placeholder";

    private readonly RenderBox _renderBox;
    private readonly VScrollBar _scrollBar;

    private CursorPos _cursorPosition;
    private CursorPos _selectionStart;

    // Cached last horizontal cursor position for vertical cursor movement.
    // Conceptually, moving the cursor up/down across "keeps" the horizontal position as much as possible.
    private float? _horizontalCursorPos;

    private ValueList<int> _lineBreaks;

    private bool _lineUpdateQueued;
    private Rope.Node _textRope = Rope.Leaf.Empty;

    public bool Editable { get; set; } = true;

    private bool _mouseSelectingText;
    private Vector2 _lastMouseSelectPos;

    // Debug overlay stuff.
    internal bool DebugOverlay;
    private Vector2? _lastDebugMousePos;

    private TextEditShared.CursorBlink _blink;

    public TextEdit()
    {
        IoCManager.InjectDependencies(this);

        AddChild(_renderBox = new RenderBox(this));
        AddChild(_scrollBar = new VScrollBar { HorizontalAlignment = HAlignment.Right });

        CanKeyboardFocus = true;
        KeyboardFocusOnClick = true;
        MouseFilter = MouseFilterMode.Stop;
    }

    public CursorPos CursorPosition
    {
        get => _cursorPosition;
        set
        {
            var clamped = MathHelper.Clamp(value.Index, 0, TextLength);
            if (TextLength != 0 && TextLength != clamped && !Rope.TryGetRuneAt(TextRope, clamped, out _))
                throw new ArgumentException("Cannot set cursor inside surrogate pair.");

            _cursorPosition = value with { Index = clamped };
            _selectionStart = _cursorPosition;
        }
    }

    public CursorPos SelectionStart
    {
        get => _selectionStart;
        set
        {
            var clamped = MathHelper.Clamp(value.Index, 0, TextLength);
            if (TextLength != 0 && TextLength != clamped && !Rope.TryGetRuneAt(TextRope, clamped, out _))
                throw new ArgumentException("Cannot set cursor inside surrogate pair.");

            _selectionStart = value with { Index = clamped };
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

    public CursorPos SelectionLower => CursorPos.Min(_selectionStart, _cursorPosition);
    public CursorPos SelectionUpper => CursorPos.Max(_selectionStart, _cursorPosition);

    public int SelectionLength => Math.Abs(_selectionStart.Index - _cursorPosition.Index);

    // TODO: cache
    public int TextLength => (int)Rope.CalcTotalLength(TextRope);

    public System.Range SelectionRange => (SelectionLower.Index)..(SelectionUpper.Index);

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

            _cursorPosition = newPos;

            if (!selectFlag)
                _selectionStart = _cursorPosition;

            if (!keepH)
                InvalidateHorizontalCursorPos();

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
                    TextRope = Rope.Delete(oldText, SelectionLower.Index, SelectionLength);
                    _cursorPosition = SelectionLower;
                    changed = true;
                }
                else if (_cursorPosition.Index != 0)
                {
                    var remPos = _cursorPosition.Index - 1;
                    var remAmt = 1;
                    // If this is a low surrogate remove two chars to remove the whole pair.
                    if (char.IsLowSurrogate(Rope.Index(oldText, remPos)))
                    {
                        remPos -= 1;
                        remAmt = 2;
                    }

                    TextRope = Rope.Delete(oldText, remPos, remAmt);
                    _cursorPosition.Index -= remAmt;
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
                    TextRope = Rope.Delete(TextRope, SelectionLower.Index, SelectionLength);
                    _cursorPosition = SelectionLower;
                    changed = true;
                }
                else if (_cursorPosition.Index < TextLength)
                {
                    var remAmt = 1;
                    if (char.IsHighSurrogate(Rope.Index(TextRope, _cursorPosition.Index)))
                        remAmt = 2;

                    TextRope = Rope.Delete(TextRope, _cursorPosition.Index, remAmt);
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
            _cursorPosition = new CursorPos(TextLength, LineBreakBias.Bottom);
            _selectionStart = new CursorPos(0, LineBreakBias.Top);

            InvalidateHorizontalCursorPos();

            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.UIClick || args.Function == EngineKeyFunctions.TextCursorSelect)
        {
            _mouseSelectingText = true;
            _lastMouseSelectPos = args.RelativePosition;

            // Find closest cursor position under mouse.
            var index = GetIndexAtPos(args.RelativePosition);

            _cursorPosition = index;

            if (args.Function != EngineKeyFunctions.TextCursorSelect)
            {
                _selectionStart = _cursorPosition;
            }

            InvalidateHorizontalCursorPos();

            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.TextCopy)
        {
            var range = SelectionRange;
            var text = Rope.CollapseSubstring(TextRope, range);
            if (text.Length != 0)
            {
                _clipboard.SetText(text);
            }

            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.TextCut)
        {
            if (Editable || SelectionLower != SelectionUpper)
            {
                var range = SelectionRange;
                var text = Rope.CollapseSubstring(TextRope, range);
                if (text.Length != 0)
                {
                    _clipboard.SetText(text);
                }

                InsertAtCursor("");
            }

            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.TextPaste)
        {
            if (Editable)
            {
                async void DoPaste()
                {
                    var text = await _clipboard.GetText();
                    InsertAtCursor(text);
                }

                DoPaste();
            }

            args.Handle();
        }
        else if (args.Function == EngineKeyFunctions.TextReleaseFocus)
        {
            ReleaseKeyboardFocus();
            args.Handle();
            return;
        }

        // Reset this so the cursor is always visible immediately after a keybind is pressed.
        _blink.Reset();

        [Pure]
        int ShiftCursorLeft()
        {
            if (_cursorPosition.Index == 0)
                return _cursorPosition.Index;

            var pos = _cursorPosition.Index - 1;

            if (char.IsLowSurrogate(Rope.Index(TextRope, pos)))
                pos -= 1;

            return pos;
        }

        [Pure]
        int ShiftCursorRight()
        {
            if (_cursorPosition.Index == TextLength)
                return _cursorPosition.Index;

            var pos = _cursorPosition.Index + 1;

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

            _horizontalCursorPos = GetHorizontalPositionAtIndex(_cursorPosition);
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
                        return SelectionLower;
                    }

                    var (_, lineStart, _) = GetLineForIndex(_cursorPosition);

                    if (_cursorPosition.Bias == LineBreakBias.Bottom && _cursorPosition.Index == lineStart)
                    {
                        return _cursorPosition with { Bias = LineBreakBias.Top };
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
                        return SelectionUpper;
                    }

                    var (_, _, lineEnd) = GetLineForIndex(_cursorPosition);
                    if (_cursorPosition.Bias == LineBreakBias.Top
                        && _cursorPosition.Index == lineEnd
                        && _cursorPosition.Index != TextLength
                        && Rope.Index(TextRope, _cursorPosition.Index) != '\n')
                    {
                        return _cursorPosition with { Bias = LineBreakBias.Bottom };
                    }

                    return new CursorPos(ShiftCursorRight(), LineBreakBias.Top);
                }
                case MoveType.LeftWord:
                {
                    var runes = Rope.EnumerateRunesReverse(TextRope, _cursorPosition.Index);
                    var pos = _cursorPosition.Index + TextEditShared.PrevWordPosition(runes.GetEnumerator());

                    return new CursorPos(pos, LineBreakBias.Bottom);
                }
                case MoveType.RightWord:
                {
                    var runes = Rope.EnumerateRunes(TextRope, _cursorPosition.Index);
                    var pos = _cursorPosition.Index + TextEditShared.NextWordPosition(runes.GetEnumerator());

                    return new CursorPos(pos, LineBreakBias.Bottom);
                }
                case MoveType.Up:
                {
                    CacheHorizontalCursorPos();

                    // TODO: From selection lower, not cursor pos.
                    var (line, _, _) = GetLineForIndex(_cursorPosition);

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

                    var (line, _, _) = GetLineForIndex(_cursorPosition);

                    // TODO: Isn't this off-by-one.
                    if (line == _lineBreaks.Count)
                    {
                        // On the last line already, move to the end of it.
                        return new CursorPos(TextLength, LineBreakBias.Top);
                    }

                    keepHorizontalCursorPos = true;

                    return GetIndexAtHorizontalPos(line + 1, _horizontalCursorPos!.Value);
                }
                case MoveType.Begin:
                {
                    var (_, lineStart, _) = GetLineForIndex(_cursorPosition);
                    if (Rope.Index(TextRope, lineStart) == '\n')
                        lineStart += 1;

                    return new CursorPos(lineStart, LineBreakBias.Bottom);
                }
                case MoveType.End:
                {
                    var (_, _, lineEnd) = GetLineForIndex(_cursorPosition);
                    return new CursorPos(lineEnd, LineBreakBias.Top);
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }

    protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (args.Function == EngineKeyFunctions.UIClick || args.Function == EngineKeyFunctions.TextCursorSelect)
        {
            _mouseSelectingText = false;
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

        // var styleBoxSize = _getStyleBox()?.MinimumSize.Y ?? 0;

        _scrollBar.Page = size.Y * UIScale;

        UpdateLineBreaks((int)(size.X * UIScale));

        return size;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _blink.FrameUpdate(args);

        if (_mouseSelectingText)
        {
            //var style = _getStyleBox();
            var contentBox = PixelSizeBox;

            /*
            if (_lastMouseSelectPos < contentBox.Left)
            {
                _drawOffset = Math.Max(0, _drawOffset - (int) Math.Ceiling(args.DeltaSeconds / MouseScrollDelay));
            }
            else if (_lastMousePosition > contentBox.Right)
            {
                // Will get clamped inside rendering code.
                _drawOffset += (int) Math.Ceiling(args.DeltaSeconds / MouseScrollDelay);
            }
            */

            var index = GetIndexAtPos(_lastMouseSelectPos);

            _cursorPosition = index;
        }

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
        var lower = SelectionLower.Index;
        var upper = SelectionUpper.Index;

        var (left, mid) = Rope.Split(TextRope, lower);
        var (_, right) = Rope.Split(mid, upper - lower);

        TextRope = Rope.Concat(left, Rope.Concat(text, right));

        //if (!InternalSetText(newContents))
        //{
        //    return;
        //}

        _selectionStart = _cursorPosition = new CursorPos(lower + text.Length, LineBreakBias.Top);
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

        // Update scroll bar max size.
        var lineCount = _lineBreaks.Count + 1;
        _scrollBar.MaxValue = Math.Max(_scrollBar.Page, lineCount * font.GetLineHeight(scale));

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

            if (line < 0)
            {
                // Above the first line, clamp.
                return (0, 0);
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

    private float GetHorizontalPositionAtIndex(CursorPos pos)
    {
        EnsureLineBreaksUpdated();

        var hPos = 0;
        var font = GetFont();

        var (_, lineStart, _) = GetLineForIndex(pos);
        using var runeEnumerator = Rope.EnumerateRunes(TextRope, lineStart).GetEnumerator();

        var i = lineStart;
        while (true)
        {
            if (i >= pos.Index)
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

    private (int lineIdx, int lineStart, int lineEnd) GetLineForIndex(CursorPos pos)
    {
        DebugTools.Assert(pos.Index >= 0);

        EnsureLineBreaksUpdated();

        if (_lineBreaks.Count == 0)
            return (0, 0, TextLength);

        int i;
        for (i = 0; i < _lineBreaks.Count; i++)
        {
            var lineIdx = _lineBreaks[i];
            if (pos.Bias == LineBreakBias.Bottom ? (lineIdx > pos.Index) : (lineIdx >= pos.Index))
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

        _lastDebugMousePos = null;
    }

    protected internal override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        _lastDebugMousePos = args.RelativePosition;
        _lastMouseSelectPos = args.RelativePosition;
    }

    protected internal override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        if (MathHelper.CloseToPercent(0, args.Delta.Y))
            return;

        _scrollBar.ValueTarget -= GetScrollSpeed() * args.Delta.Y;
    }

    [System.Diagnostics.Contracts.Pure]
    private float GetScrollSpeed()
    {
        return OutputPanel.GetScrollSpeed(GetFont(), UIScale);
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
            if (_master.DebugOverlay && _master._lastDebugMousePos is { } mouse)
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

            var scrollOffset = -_master._scrollBar.Value;
            var scale = UIScale;
            var baseLine = new Vector2(0, scrollOffset + font.GetAscent(scale));
            var height = font.GetLineHeight(scale);
            var descent = font.GetDescent(scale);

            var lineBreakIndex = 0;

            var count = 0;

            var selectionLower = _master.SelectionLower;
            var selectionUpper = _master.SelectionUpper;

            int? selectStartPos = null;
            int? selectEndPos = null;
            var selecting = false;

            foreach (var rune in Rope.EnumerateRunes(_master.TextRope))
            {
                CheckDrawCursors(LineBreakBias.Top);

                if (lineBreakIndex < _master._lineBreaks.Count
                    && _master._lineBreaks[lineBreakIndex] == count)
                {
                    // Line break
                    // Check to handle

                    PostDrawLine();

                    baseLine = new Vector2(drawBox.Left, baseLine.Y + font.GetLineHeight(scale));
                    lineBreakIndex += 1;

                    selectStartPos = selecting ? 0 : null;
                    selectEndPos = null;
                }

                CheckDrawCursors(LineBreakBias.Bottom);

                var color = Color.White;
                baseLine.X += font.DrawChar(handle, rune, baseLine, scale, color);

                count += rune.Utf16SequenceLength;
            }

            // Also draw cursor if it's at the very end.
            CheckDrawCursors(LineBreakBias.Bottom);
            CheckDrawCursors(LineBreakBias.Top);
            PostDrawLine();

            // Draw cursor bias
            if (_master.DebugOverlay)
            {
                var arrow = _master.CursorPosition.Bias == LineBreakBias.Bottom ? ArrowDown : ArrowUp;
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

                if (_master._cursorPosition.Index == count
                    && _master.HasKeyboardFocus()
                    && _master._blink.CurrentlyLit
                    && _master._cursorPosition.Bias == bias)
                {
                    handle.DrawRect(
                        new UIBox2(
                            baseLine.X,
                            baseLine.Y - height + descent,
                            baseLine.X + 1,
                            baseLine.Y + descent),
                        Color.White);
                }

                if (selectionLower.Index == count && selectionLower.Bias == bias)
                {
                    selecting = true;
                    selectStartPos = (int)baseLine.X;
                }

                if (selectionUpper.Index == count & selectionUpper.Bias == bias)
                {
                    selecting = false;
                    selectEndPos = (int)baseLine.X;
                }
            }

            void PostDrawLine()
            {
                if (selectStartPos == null)
                    return;

                var rect = new UIBox2(
                    selectStartPos.Value,
                    baseLine.Y - height + descent,
                    selectEndPos ?? baseLine.X,
                    baseLine.Y + descent
                );

                var color = _master.StylePropertyDefault(
                    StylePropertySelectionColor,
                    Color.CornflowerBlue.WithAlpha(0.25f));

                handle.DrawRect(rect, color);
            }
        }
    }

    /// <summary>
    /// Specifies which line the cursor is positioned at when on a word-wrapping break.
    /// </summary>
    public enum LineBreakBias : byte
    {
        // @formatter:off
        Top    = 0,
        Bottom = 1
        // @formatter:on
    }

    /// <summary>
    /// Stores the necessary data for a position in the cursor of the text.
    /// </summary>
    /// <param name="Index">The index of the cursor in the text contents.</param>
    /// <param name="Bias">Which direction to bias the cursor to </param>
    public record struct CursorPos(int Index, LineBreakBias Bias) : IComparable<CursorPos>
    {
        public static CursorPos Min(CursorPos a, CursorPos b)
        {
            var cmp = a.CompareTo(b);
            if (cmp < 0)
                return a;

            return b;
        }

        public static CursorPos Max(CursorPos a, CursorPos b)
        {
            var cmp = a.CompareTo(b);
            if (cmp > 0)
                return a;

            return b;
        }

        public int CompareTo(CursorPos other)
        {
            var indexComparison = Index.CompareTo(other.Index);
            if (indexComparison != 0)
                return indexComparison;

            // If two positions are at the same index, the one with bias top is considered earlier.
            return ((byte)Bias).CompareTo((byte)other.Bias);
        }

        public static bool operator <(CursorPos left, CursorPos right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(CursorPos left, CursorPos right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(CursorPos left, CursorPos right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(CursorPos left, CursorPos right)
        {
            return left.CompareTo(right) >= 0;
        }
    }

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
