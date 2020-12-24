using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{
    /// <summary>
    ///     Allows the user to input and modify a line of text.
    /// </summary>
    public class LineEdit : Control
    {
        private const float BlinkTime = 0.5f;
        private const float MouseScrollDelay = 0.001f;

        public const string StylePropertyStyleBox = "stylebox";
        public const string StyleClassLineEditNotEditable = "notEditable";
        public const string StylePseudoClassPlaceholder = "placeholder";

        private int _cursorPosition;
        private int _selectionStart;
        private string _text = "";
        private bool _editable = true;
        private string? _placeHolder;

        private int _drawOffset;

        private float _cursorBlinkTimer;
        private bool _cursorCurrentlyLit;
        private readonly LineEditRenderBox _renderBox;

        private bool _mouseSelectingText;
        private float _lastMousePosition;

        private bool IsPlaceHolderVisible => string.IsNullOrEmpty(_text) && _placeHolder != null;

        public event Action<LineEditEventArgs>? OnTextChanged;
        public event Action<LineEditEventArgs>? OnTextEntered;
        public event Action<LineEditEventArgs>? OnFocusEnter;
        public event Action<LineEditEventArgs>? OnFocusExit;

        /// <summary>
        ///     Determines whether the LineEdit text gets changed by the input text.
        /// </summary>
        public Func<string, bool>? IsValid { get; set; }

        /// <summary>
        ///     The actual text currently stored in the LineEdit.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string Text
        {
            get => _text;
            set
            {
                if (value == null)
                {
                    value = "";
                }

                if (!SetText(value))
                {
                    return;
                }

                _cursorPosition = 0;
                _selectionStart = 0;
                _updatePseudoClass();
            }
        }

        /// <summary>
        ///     The text
        /// </summary>
        public ReadOnlySpan<char> SelectedText
        {
            get
            {
                var lower = SelectionLower;

                return _text.AsSpan(lower, SelectionLength);
            }
        }

        public int SelectionLength => Math.Abs(_selectionStart - _cursorPosition);

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Editable
        {
            get => _editable;
            set
            {
                _editable = value;
                if (_editable)
                {
                    DefaultCursorShape = CursorShape.IBeam;
                    RemoveStyleClass(StyleClassLineEditNotEditable);
                }
                else
                {
                    DefaultCursorShape = CursorShape.Arrow;
                    AddStyleClass(StyleClassLineEditNotEditable);
                }
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public string? PlaceHolder
        {
            get => _placeHolder;
            set
            {
                _placeHolder = value;
                _updatePseudoClass();
            }
        }

        public int CursorPosition
        {
            get => _cursorPosition;
            set
            {
                _cursorPosition = MathHelper.Clamp(value, 0, _text.Length);
                _selectionStart = _cursorPosition;
            }
        }

        public int SelectionStart
        {
            get => _selectionStart;
            set => _selectionStart = MathHelper.Clamp(value, 0, _text.Length);
        }

        public int SelectionLower => Math.Min(_selectionStart, _cursorPosition);
        public int SelectionUpper => Math.Max(_selectionStart, _cursorPosition);

        public bool IgnoreNext { get; set; }

        // TODO:
        // I decided to not implement the entire LineEdit API yet,
        // since most of it won't be used yet (if at all).
        // Feel free to implement wrappers for all the other properties!
        // Future me reporting, thanks past me.
        // Second future me reporting, thanks again.
        // Third future me is here to say thanks.
        // Fourth future me is here to continue the tradition.

        public LineEdit()
        {
            MouseFilter = MouseFilterMode.Stop;
            CanKeyboardFocus = true;
            KeyboardFocusOnClick = true;

            DefaultCursorShape = CursorShape.IBeam;

            AddChild(_renderBox = new LineEditRenderBox(this));
        }

        public void Clear()
        {
            Text = "";
        }

        public void InsertAtCursor(string text)
        {
            // Strip newlines.
            var chars = new List<char>(text.Length);
            foreach (var chr in text)
            {
                if (chr == '\n')
                {
                    continue;
                }

                chars.Add(chr);
            }

            text = new string(chars.ToArray());

            var lower = SelectionLower;
            var newContents = Text[..lower] + text + Text[SelectionUpper..];

            if (!SetText(newContents))
            {
                return;
            }

            _selectionStart = _cursorPosition = lower + chars.Count;
            OnTextChanged?.Invoke(new LineEditEventArgs(this, _text));
            _updatePseudoClass();
        }

        /// <remarks>
        /// Does not fix cursor positions, those will have to be adjusted manually.
        /// </remarks>>
        protected bool SetText(string newText)
        {
            if (IsValid != null && !IsValid(newText))
            {
                return false;
            }

            _text = newText;
            return true;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);

            _cursorBlinkTimer -= args.DeltaSeconds;
            if (_cursorBlinkTimer <= 0)
            {
                _cursorBlinkTimer += BlinkTime;
                _cursorCurrentlyLit = !_cursorCurrentlyLit;
            }

            if (_mouseSelectingText)
            {
                var style = _getStyleBox();
                var contentBox = style.GetContentBox(PixelSizeBox);

                if (_lastMousePosition < contentBox.Left)
                {
                    _drawOffset = Math.Max(0, _drawOffset - (int) Math.Ceiling(args.DeltaSeconds / MouseScrollDelay));
                }
                else if (_lastMousePosition > contentBox.Right)
                {
                    // Will get clamped inside rendering code.
                    _drawOffset += (int) Math.Ceiling(args.DeltaSeconds / MouseScrollDelay);
                }

                var index = GetIndexAtPos(MathHelper.Clamp(_lastMousePosition, contentBox.Left, contentBox.Right));

                _cursorPosition = index;
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            var font = _getFont();
            var style = _getStyleBox();
            return new Vector2(0, font.GetHeight(UIScale) / UIScale) + style.MinimumSize / UIScale;
        }

        protected override void LayoutUpdateOverride()
        {
            var style = _getStyleBox();

            FitChildInPixelBox(_renderBox, (UIBox2i) style.GetContentBox(PixelSizeBox));
        }

        protected internal override void TextEntered(GUITextEventArgs args)
        {
            base.TextEntered(args);

            if (!Editable)
            {
                return;
            }

            if (IgnoreNext)
            {
                IgnoreNext = false;
                return;
            }

            InsertAtCursor(((char) args.CodePoint).ToString());
        }

        protected internal override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (args.Function != EngineKeyFunctions.UIClick && args.Function != EngineKeyFunctions.TextCursorSelect)
            {
                if (!HasKeyboardFocus())
                {
                    return;
                }

                if (args.Function == EngineKeyFunctions.TextBackspace)
                {
                    if (Editable)
                    {
                        var changed = false;
                        if (_selectionStart != _cursorPosition)
                        {
                            _text = _text.Remove(SelectionLower, SelectionLength);
                            _cursorPosition = SelectionLower;
                            changed = true;
                        }
                        else if (_cursorPosition != 0)
                        {
                            _text = _text.Remove(_cursorPosition - 1, 1);
                            _cursorPosition -= 1;
                            changed = true;
                        }

                        if (changed)
                        {
                            _selectionStart = _cursorPosition;
                            OnTextChanged?.Invoke(new LineEditEventArgs(this, _text));
                            _updatePseudoClass();
                        }
                    }

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextDelete)
                {
                    if (Editable)
                    {
                        var changed = false;
                        if (_selectionStart != _cursorPosition)
                        {
                            _text = _text.Remove(SelectionLower, SelectionLength);
                            _cursorPosition = SelectionLower;
                            changed = true;
                        }
                        else if (_cursorPosition < _text.Length)
                        {
                            _text = _text.Remove(_cursorPosition, 1);
                            changed = true;
                        }

                        if (changed)
                        {
                            _selectionStart = _cursorPosition;
                            OnTextChanged?.Invoke(new LineEditEventArgs(this, _text));
                            _updatePseudoClass();
                        }
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
                        if (_cursorPosition != 0)
                        {
                            _cursorPosition -= 1;
                        }

                        _selectionStart = _cursorPosition;
                    }

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorRight)
                {
                    if (_selectionStart != _cursorPosition)
                    {
                        _cursorPosition = _selectionStart = SelectionUpper;
                    }
                    else
                    {
                        if (_cursorPosition != _text.Length)
                        {
                            _cursorPosition += 1;
                        }

                        _selectionStart = _cursorPosition;
                    }

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorWordLeft)
                {
                    _selectionStart = _cursorPosition = PrevWordPosition(_text, _cursorPosition);

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorWordRight)
                {
                    _selectionStart = _cursorPosition = NextWordPosition(_text, _cursorPosition);

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorBegin)
                {
                    _selectionStart = _cursorPosition = 0;
                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorEnd)
                {
                    _selectionStart = _cursorPosition = _text.Length;
                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorSelectLeft)
                {
                    if (_cursorPosition != 0)
                    {
                        _cursorPosition -= 1;
                    }

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorSelectRight)
                {
                    if (_cursorPosition != _text.Length)
                    {
                        _cursorPosition += 1;
                    }

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorSelectWordLeft)
                {
                    _cursorPosition = PrevWordPosition(_text, _cursorPosition);

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorSelectWordRight)
                {
                    _cursorPosition = NextWordPosition(_text, _cursorPosition);

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorSelectBegin)
                {
                    _cursorPosition = 0;
                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorSelectEnd)
                {
                    _cursorPosition = _text.Length;
                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextSubmit)
                {
                    if (Editable)
                    {
                        OnTextEntered?.Invoke(new LineEditEventArgs(this, _text));
                    }

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextPaste)
                {
                    if (Editable)
                    {
                        var clipboard = IoCManager.Resolve<IClipboardManager>();
                        var text = clipboard.GetText();
                        if (text != null)
                        {
                            InsertAtCursor(text);
                        }
                    }

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCut)
                {
                    if (Editable || SelectionLower != SelectionUpper)
                    {
                        var clipboard = IoCManager.Resolve<IClipboardManager>();
                        var text = SelectedText;
                        if (!text.IsEmpty)
                        {
                            clipboard.SetText(SelectedText.ToString());
                        }

                        InsertAtCursor("");
                    }

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCopy)
                {
                    var clipboard = IoCManager.Resolve<IClipboardManager>();
                    var text = SelectedText;
                    if (!text.IsEmpty)
                    {
                        clipboard.SetText(text.ToString());
                    }

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextSelectAll)
                {
                    _cursorPosition = _text.Length;
                    _selectionStart = 0;
                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextReleaseFocus)
                {
                    ReleaseKeyboardFocus();
                    args.Handle();
                    return;
                }
            }
            else
            {
                _mouseSelectingText = true;
                _lastMousePosition = args.RelativePosition.X;

                // Find closest cursor position under mouse.
                var index = GetIndexAtPos(args.RelativePosition.X);

                _cursorPosition = index;

                if (args.Function != EngineKeyFunctions.TextCursorSelect)
                {
                    _selectionStart = index;
                }

                args.Handle();
            }

            // Reset this so the cursor is always visible immediately after a keybind is pressed.
            _resetCursorBlink();
        }

        protected internal override void KeyBindUp(GUIBoundKeyEventArgs args)
        {
            base.KeyBindUp(args);

            if (args.Function == EngineKeyFunctions.UIClick || args.Function == EngineKeyFunctions.TextCursorSelect)
            {
                _mouseSelectingText = false;
            }
        }

        protected internal override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);

            _lastMousePosition = args.RelativePosition.X;
        }

        private int GetIndexAtPos(float horizontalPos)
        {
            var style = _getStyleBox();
            var contentBox = style.GetContentBox(PixelSizeBox);

            var clickPosX = horizontalPos * UIScale;

            var font = _getFont();
            var index = 0;
            var chrPosX = contentBox.Left - _drawOffset;
            var lastChrPostX = contentBox.Left - _drawOffset;
            foreach (var chr in _text)
            {
                if (!font.TryGetCharMetrics(chr, UIScale, out var metrics))
                {
                    index += 1;
                    continue;
                }

                if (chrPosX > clickPosX)
                {
                    break;
                }

                lastChrPostX = chrPosX;
                chrPosX += metrics.Advance;
                index += 1;

                if (chrPosX > contentBox.Right)
                {
                    break;
                }
            }

            // Distance between the right side of the glyph overlapping the mouse and the mouse.
            var distanceRight = chrPosX - clickPosX;
            // Same but left side.
            var distanceLeft = clickPosX - lastChrPostX;
            // If the mouse is closer to the left of the glyph we lower the index one, so we select before that glyph.
            if (index > 0 && distanceRight > distanceLeft)
            {
                index -= 1;
            }

            return index;
        }

        protected internal override void KeyboardFocusEntered()
        {
            base.KeyboardFocusEntered();

            // Reset this so the cursor is always visible immediately after gaining focus..
            _resetCursorBlink();
            OnFocusEnter?.Invoke(new LineEditEventArgs(this, _text));
        }

        protected internal override void KeyboardFocusExited()
        {
            base.KeyboardFocusExited();
            OnFocusExit?.Invoke(new LineEditEventArgs(this, _text));
        }

        [Pure]
        private Font _getFont()
        {
            if (TryGetStyleProperty<Font>("font", out var font))
            {
                return font;
            }

            return UserInterfaceManager.ThemeDefaults.DefaultFont;
        }

        [Pure]
        private StyleBox _getStyleBox()
        {
            if (TryGetStyleProperty<StyleBox>(StylePropertyStyleBox, out var box))
            {
                return box;
            }

            return UserInterfaceManager.ThemeDefaults.LineEditBox;
        }

        [Pure]
        private Color _getFontColor()
        {
            if (TryGetStyleProperty("font-color", out Color color))
            {
                return color;
            }

            return Color.White;
        }

        private void _updatePseudoClass()
        {
            SetOnlyStylePseudoClass(IsPlaceHolderVisible ? StylePseudoClassPlaceholder : null);
        }

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            base.Draw(handle);

            _getStyleBox().Draw(handle, PixelSizeBox);
        }

        // Approach for NextWordPosition and PrevWordPosition taken from Avalonia.
        private int NextWordPosition(string str, int cursor)
        {
            if (cursor >= str.Length)
            {
                return str.Length;
            }

            var charClass = GetCharClass(str[cursor]);

            var i = cursor;
            for (; i < str.Length && GetCharClass(str[i]) == charClass; i++)
            {
            }

            for (; i < str.Length && GetCharClass(str[i]) == CharClass.Whitespace; i++)
            {
            }

            return i;
        }

        private int PrevWordPosition(string str, int cursor)
        {
            if (cursor == 0)
            {
                return 0;
            }

            var charClass = GetCharClass(str[cursor - 1]);

            var i = cursor;
            for (; i > 0 && GetCharClass(str[i - 1]) == charClass; i--)
            {
            }

            if (charClass == CharClass.Whitespace)
            {
                charClass = GetCharClass(str[i - 1]);
                for (; i > 0 && GetCharClass(str[i - 1]) == charClass; i--)
                {
                }
            }

            return i;
        }

        private CharClass GetCharClass(char chr)
        {
            if (char.IsWhiteSpace(chr))
            {
                return CharClass.Whitespace;
            }

            if (char.IsLetterOrDigit(chr))
            {
                return CharClass.AlphaNumeric;
            }

            return CharClass.Other;
        }

        private enum CharClass : byte
        {
            Other,
            AlphaNumeric,
            Whitespace
        }

        public class LineEditEventArgs : EventArgs
        {
            public LineEdit Control { get; }
            public string Text { get; }

            public LineEditEventArgs(LineEdit control, string text)
            {
                Control = control;
                Text = text;
            }
        }

        /// <summary>
        ///     Use a separate control to do the rendering to make use of RectClipContent,
        ///     so that we can clip characters in half.
        /// </summary>
        private sealed class LineEditRenderBox : Control
        {
            private readonly LineEdit _master;

            public LineEditRenderBox(LineEdit master)
            {
                _master = master;

                RectClipContent = true;
            }

            protected internal override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);

                var contentBox = PixelSizeBox;
                var font = _master._getFont();
                var renderedTextColor = _master._getFontColor();

                var offsetY = (contentBox.Height - font.GetHeight(UIScale)) / 2;

                var renderedText = _master.IsPlaceHolderVisible ? _master._placeHolder! : _master._text;
                DebugTools.AssertNotNull(renderedText);

                ref var drawOffset = ref _master._drawOffset;

                // Go through the entire text once to find length/positional data of cursor.
                var count = 0;
                var posX = 0;
                var actualCursorPosition = 0;
                var actualSelectionStartPosition = 0;
                foreach (var chr in renderedText)
                {
                    if (!font.TryGetCharMetrics(chr, UIScale, out var metrics))
                    {
                        count += 1;
                        continue;
                    }

                    posX += metrics.Advance;
                    count += 1;

                    if (count == _master._cursorPosition)
                    {
                        actualCursorPosition = posX;
                    }

                    if (count == _master._selectionStart)
                    {
                        actualSelectionStartPosition = posX;
                    }
                }

                var totalLength = posX;

                // Shift drawOffset around to fill as much as possible.
                var end = totalLength - drawOffset;
                if (end + 1 < contentBox.Width)
                {
                    drawOffset = Math.Max(0, drawOffset - (contentBox.Width - end));
                }

                // Shift drawOffset around so that cursor is always visible.
                if (actualCursorPosition < drawOffset)
                {
                    drawOffset -= drawOffset - actualCursorPosition;
                }
                else if (actualCursorPosition >= contentBox.Width + drawOffset)
                {
                    drawOffset += actualCursorPosition - (contentBox.Width + drawOffset - 1);
                }

                // Apply drawOffset to positional data.
                actualCursorPosition -= drawOffset;
                actualSelectionStartPosition -= drawOffset;

                // Actually render.
                var baseLine = (-drawOffset, offsetY + font.GetAscent(UIScale)) +
                               contentBox.TopLeft;

                foreach (var chr in renderedText)
                {
                    if (!font.TryGetCharMetrics(chr, UIScale, out var metrics))
                    {
                        continue;
                    }

                    if (baseLine.X > contentBox.Right)
                    {
                        // Past the right edge, not gonna render anything anymore.
                        break;
                    }

                    // Make sure we're not off the left edge of the box.
                    if (baseLine.X + metrics.BearingX + metrics.Width >= contentBox.Left)
                    {
                        font.DrawChar(handle, chr, baseLine, UIScale, renderedTextColor);
                    }

                    baseLine += (metrics.Advance, 0);
                }

                // Draw cursor/selection.
                if (_master.HasKeyboardFocus())
                {
                    var selectionLower = Math.Min(actualSelectionStartPosition, actualCursorPosition);
                    var selectionUpper = Math.Max(actualSelectionStartPosition, actualCursorPosition);

                    if (selectionLower != selectionUpper)
                    {
                        handle.DrawRect(new UIBox2(selectionLower, contentBox.Top, selectionUpper, contentBox.Bottom),
                            Color.CornflowerBlue.WithAlpha(0.25f));
                    }

                    if (_master._cursorCurrentlyLit)
                    {
                        handle.DrawRect(
                            new UIBox2(actualCursorPosition, contentBox.Top, actualCursorPosition + 1,
                                contentBox.Bottom), Color.White);
                    }
                }
            }
        }

        private void _resetCursorBlink()
        {
            _cursorCurrentlyLit = true;
            _cursorBlinkTimer = BlinkTime;
        }
    }
}
