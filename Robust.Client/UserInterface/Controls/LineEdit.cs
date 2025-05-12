using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Shared;
using Robust.Shared.Configuration;
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
    [Virtual]
    public class LineEdit : Control
    {
        [Dependency] private readonly IConfigurationManager _cfgManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;

        private const float MouseScrollDelay = 0.001f;

        public const string StylePropertyStyleBox = "stylebox";
        public const string StylePropertyCursorColor = "cursor-color";
        public const string StylePropertySelectionColor = "selection-color";
        public const string StyleClassLineEditNotEditable = "notEditable";
        public const string StylePseudoClassPlaceholder = "placeholder";

        public StyleBox? StyleBoxOverride { get; set; }

        // It is assumed that these two positions are NEVER inside a surrogate pair in the text buffer.
        private int _cursorPosition;
        private int _selectionStart;
        private string _text = "";
        private bool _editable = true;
        private string? _placeHolder;

        private int _drawOffset;

        private TextEditShared.CursorBlink _blink;
        private readonly LineEditRenderBox _renderBox;

        private bool _mouseSelectingText;
        private float _lastMousePosition;

        private TimeSpan? _lastClickTime;
        private Vector2? _lastClickPosition;

        // Keep track of the frame on which we got focus, so we can implement SelectAllOnFocus properly.
        // Otherwise, there's no way to keep track of whether the KeyDown is the one that focused the text box,
        // to avoid text selection stomping on the behavior.
        // This isn't a great way to do it.
        // A better fix would be to annotate all input events with some unique sequence ID,
        // and expose the input event that focused the control in KeyboardFocusEntered.
        // But that sounds like a refactor I'm not doing today.
        private uint _focusedOnFrame;

        private bool IsPlaceHolderVisible => !(HidePlaceHolderOnFocus && HasKeyboardFocus()) && string.IsNullOrEmpty(_text) && _placeHolder != null;

        public event Action<LineEditEventArgs>? OnTextChanged;
        public event Action<LineEditEventArgs>? OnTextEntered;
        public event Action<LineEditEventArgs>? OnFocusEnter;
        public event Action<LineEditEventArgs>? OnFocusExit;
        public event Action<LineEditEventArgs>? OnTabComplete;

        /// <summary>
        ///     Determines whether the LineEdit text gets changed by the input text.
        /// </summary>
        public Func<string, bool>? IsValid { get; set; }

        /// <summary>
        ///     The actual text currently stored in the LineEdit. Setting this does not invoke the text change event, use <see cref="SetText(string, bool)"/> for that.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string Text
        {
            get => _text;
            set => SetText(value);
        }

        /// <summary>
        ///     Sets the current text, optionally invoking <see cref="OnTextChanged"/>.
        /// </summary>
        public void SetText(string value, bool invokeEvent = false)
        {
            if (value == _text)
                return;

            // Save cursor position or -1 for end
            var cursorTarget = CursorPosition == _text.Length ? -1 : CursorPosition;

            if (!InternalSetText(value))
            {
                return;
            }

            var clamped = MathHelper.Clamp(cursorTarget == -1 ? _text.Length : cursorTarget, 0, _text.Length);
            while (clamped < _text.Length && !Rune.TryGetRuneAt(_text, clamped, out _))
            {
                clamped++;
            }

            _cursorPosition = clamped;
            _selectionStart = _cursorPosition;
            _updatePseudoClass();
            if (invokeEvent)
                OnTextChanged?.Invoke(new LineEditEventArgs(this, _text));
        }

        public void ForceSubmitText()
        {
            OnTextEntered?.Invoke(new LineEditEventArgs(this, _text));
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
                var clamped = MathHelper.Clamp(value, 0, _text.Length);
                if (_text.Length != 0 && _text.Length != clamped && !Rune.TryGetRuneAt(_text, clamped, out _))
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
                var clamped = MathHelper.Clamp(value, 0, _text.Length);
                if (_text.Length != 0 && _text.Length != clamped && !Rune.TryGetRuneAt(_text, clamped, out _))
                    throw new ArgumentException("Cannot set cursor inside surrogate pair.");

                _selectionStart = clamped;
            }
        }

        public int SelectionLower => Math.Min(_selectionStart, _cursorPosition);
        public int SelectionUpper => Math.Max(_selectionStart, _cursorPosition);

        public bool HidePlaceHolderOnFocus { get; set; }

        public bool IgnoreNext { get; set; }

        /// <summary>
        /// If true, all the text in the LineEdit will be automatically selected whenever it is focused.
        /// </summary>
        public bool SelectAllOnFocus { get; set; }

        private (int start, int length)? _imeData;


        // TODO:
        // I decided to not implement the entire LineEdit API yet,
        // since most of it won't be used yet (if at all).
        // Feel free to implement wrappers for all the other properties!
        // Future me reporting, thanks past me.
        // Second future me reporting, thanks again.
        // Third future me is here to say thanks.
        // Fourth future me is here to continue the tradition.
        // Fifth future me is unsure what this is even about but continues to be grateful.

        public LineEdit()
        {
            IoCManager.InjectDependencies(this);

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

            if (!InternalSetText(newContents))
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
        private bool InternalSetText(string newText)
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

            IgnoreNext = false;

            _blink.FrameUpdate(args);

            if (_mouseSelectingText)
            {
                var style = _getStyleBox();
                var contentBox = style.GetContentBox(PixelSizeBox, UIScale);

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

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            var font = _getFont();
            var style = _getStyleBox();
            return new Vector2(0, font.GetHeight(1.0f)) + style.MinimumSize;
        }

        protected override Vector2 ArrangeOverride(Vector2 finalSize)
        {
            var style = _getStyleBox();
            var box = UIBox2.FromDimensions(Vector2.Zero, finalSize);
            _renderBox.Arrange(style.GetContentBox(box, 1));

            return finalSize;
        }

        public event Action<GUITextEnteredEventArgs>? OnTextTyped;

        protected internal override void TextEntered(GUITextEnteredEventArgs args)
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

            InsertAtCursor(args.Text);
            OnTextTyped?.Invoke(args);
        }

        protected internal override void TextEditing(GUITextEditingEventArgs args)
        {
            base.TextEditing(args);

            if (!Editable)
                return;

            // TODO: yeah so uh this ignores all the valid checks and everything like that.
            // Uh....

            var ev = args.Event;
            var startChars = ev.GetStartChars();

            // Just break an active composition and build it anew to handle in-progress ones.
            AbortIme();

            if (ev.Text != "")
            {
                if (_selectionStart != _cursorPosition)
                {
                    // Delete active text selection.
                    InsertAtCursor("");
                }

                var startPos = _cursorPosition;
                _text = _text[..startPos] + ev.Text + _text[startPos..];

                _selectionStart = _cursorPosition = startPos + startChars;
                _imeData = (startPos, ev.Text.Length);

                _updatePseudoClass();
            }
        }

        private void AbortIme(bool delete = true)
        {
            if (!_imeData.HasValue)
                return;

            if (delete)
            {
                var (imeStart, imeLength) = _imeData.Value;

                _text = _text.Remove(imeStart, imeLength);

                _selectionStart = _cursorPosition = imeStart;

                _updatePseudoClass();
            }

            _imeData = null;
        }

        public event Action<LineEditTextRemovedEventArgs>? OnTextRemoved;

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
                        var oldText = _text;
                        var cursor = _cursorPosition;
                        var selectStart = _selectionStart;
                        if (_selectionStart != _cursorPosition)
                        {
                            _text = _text.Remove(SelectionLower, SelectionLength);
                            _cursorPosition = SelectionLower;
                            changed = true;
                        }
                        else if (_cursorPosition != 0)
                        {
                            var remPos = _cursorPosition - 1;
                            var remAmt = 1;
                            // If this is a low surrogate remove two chars to remove the whole pair.
                            if (char.IsLowSurrogate(_text[remPos]))
                            {
                                remPos -= 1;
                                remAmt = 2;
                            }
                            _text = _text.Remove(remPos, remAmt);
                            _cursorPosition -= remAmt;
                            changed = true;
                        }

                        if (changed)
                        {
                            _selectionStart = _cursorPosition;
                            OnTextChanged?.Invoke(new LineEditEventArgs(this, _text));
                            _updatePseudoClass();
                            OnTextRemoved?.Invoke(new LineEditTextRemovedEventArgs(oldText, _text, cursor, selectStart));
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
                            var remAmt = 1;
                            if (char.IsHighSurrogate(_text[_cursorPosition]))
                                remAmt = 2;
                            _text = _text.Remove(_cursorPosition, remAmt);
                            changed = true;
                        }

                        if (changed)
                        {
                            _selectionStart = _cursorPosition;
                            OnTextChanged?.Invoke(new LineEditEventArgs(this, _text));
                            _updatePseudoClass();
                            OnTextRemoved?.Invoke(new LineEditTextRemovedEventArgs(_text, _text, _cursorPosition, _selectionStart));
                        }
                    }

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextWordBackspace)
                {
                    if (Editable)
                    {
                        var changed = false;

                        // If there is a selection, we just delete the selection. Otherwise we delete the previous word
                        if (_selectionStart != _cursorPosition)
                        {
                            _text = _text.Remove(SelectionLower, SelectionLength);
                            _cursorPosition = SelectionLower;
                            changed = true;
                        }
                        else if (_cursorPosition != 0)
                        {
                            int remAmt = _cursorPosition - TextEditShared.PrevWordPosition(_text, _cursorPosition);

                            _text = _text.Remove(_cursorPosition - remAmt, remAmt);
                            _cursorPosition -= remAmt;
                            changed = true;
                        }

                        if (changed)
                        {
                            _selectionStart = _cursorPosition;
                            OnTextChanged?.Invoke(new LineEditEventArgs(this, _text));
                            _updatePseudoClass();
                            OnTextRemoved?.Invoke(new LineEditTextRemovedEventArgs(_text, _text, _cursorPosition, _selectionStart));
                        }
                    }

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextWordDelete)
                {
                    if (Editable)
                    {
                        var changed = false;

                        // If there is a selection, we just delete the selection. Otherwise we delete the next word
                        if (_selectionStart != _cursorPosition)
                        {
                            _text = _text.Remove(SelectionLower, SelectionLength);
                            _cursorPosition = SelectionLower;
                            changed = true;
                        }
                        else if (_cursorPosition < _text.Length)
                        {
                            int nextWord = TextEditShared.EndWordPosition(_text, _cursorPosition);
                            _text = _text.Remove(_cursorPosition, nextWord - _cursorPosition);
                            changed = true;
                        }

                        if (changed)
                        {
                            _selectionStart = _cursorPosition;
                            OnTextChanged?.Invoke(new LineEditEventArgs(this, _text));
                            _updatePseudoClass();
                            OnTextRemoved?.Invoke(new LineEditTextRemovedEventArgs(_text, _text, _cursorPosition, _selectionStart));
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
                        ShiftCursorLeft();

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
                        ShiftCursorRight();

                        _selectionStart = _cursorPosition;
                    }

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorWordLeft)
                {
                    _selectionStart = _cursorPosition = TextEditShared.PrevWordPosition(_text, _cursorPosition);

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorWordRight)
                {
                    _selectionStart = _cursorPosition = TextEditShared.EndWordPosition(_text, _cursorPosition);

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
                    ShiftCursorLeft();

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorSelectRight)
                {
                    ShiftCursorRight();

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorSelectWordLeft)
                {
                    _cursorPosition = TextEditShared.PrevWordPosition(_text, _cursorPosition);

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextCursorSelectWordRight)
                {
                    _cursorPosition = TextEditShared.EndWordPosition(_text, _cursorPosition);

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
                        ForceSubmitText();
                    }

                    args.Handle();
                }
                else if (args.Function == EngineKeyFunctions.TextPaste)
                {
                    if (Editable)
                    {
                        async void DoPaste()
                        {
                            var clipboard = IoCManager.Resolve<IClipboardManager>();
                            var text = await clipboard.GetText();
                            if (text != null)
                            {
                                InsertAtCursor(text);
                            }
                        }

                        DoPaste();
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
                else if (args.Function == EngineKeyFunctions.TextTabComplete)
                {
                    if (Editable)
                    {
                        OnTabComplete?.Invoke(new LineEditEventArgs(this, _text));
                    }

                    args.Handle();
                }
            }
            // Double-clicking. Clicks delay should be <= 250ms and the distance < 10 pixels.
            else if (args.Function == EngineKeyFunctions.UIClick && _lastClickPosition != null && _lastClickTime != null
                     && _timing.RealTime - _lastClickTime <= TimeSpan.FromMilliseconds(_cfgManager.GetCVar(CVars.DoubleClickDelay))
                     && (_lastClickPosition.Value - args.PointerLocation.Position).IsShorterThan(_cfgManager.GetCVar(CVars.DoubleClickRange)))
            {
                _lastClickTime = _timing.RealTime;
                _lastClickPosition = args.PointerLocation.Position;

                _lastMousePosition = args.RelativePosition.X;

                _selectionStart = TextEditShared.PrevWordPosition(_text, GetIndexAtPos(args.RelativePosition.X));
                _cursorPosition = TextEditShared.EndWordPosition(_text, GetIndexAtPos(args.RelativePosition.X));

                args.Handle();
            }
            else if (!(SelectAllOnFocus && _focusedOnFrame == _timing.CurFrame))
            {
                _lastClickTime = _timing.RealTime;
                _lastClickPosition = args.PointerLocation.Position;

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
            _blink.Reset();

            void ShiftCursorLeft()
            {
                if (_cursorPosition == 0)
                    return;

                _cursorPosition -= 1;

                if (char.IsLowSurrogate(_text[_cursorPosition]))
                    _cursorPosition -= 1;
            }

            void ShiftCursorRight()
            {
                if (_cursorPosition == _text.Length)
                    return;

                _cursorPosition += 1;

                // Before you confuse yourself on "shouldn't this be high surrogate since shifting left checks low"
                // (Because yes, I did myself too a week after writing it)
                // char.IsLowSurrogate(_text[_cursorPosition]) means "is the cursor between a surrogate pair"
                // because we ALREADY moved.
                if (_cursorPosition != _text.Length && char.IsLowSurrogate(_text[_cursorPosition]))
                    _cursorPosition += 1;
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

        protected internal override void MouseMove(GUIMouseMoveEventArgs args)
        {
            base.MouseMove(args);

            _lastMousePosition = args.RelativePosition.X;
        }

        private int GetIndexAtPos(float horizontalPos)
        {
            var style = _getStyleBox();
            var contentBox = style.GetContentBox(PixelSizeBox, UIScale);

            var clickPosX = horizontalPos * UIScale;

            var font = _getFont();
            var index = 0;
            var chrPosX = contentBox.Left - _drawOffset;
            var lastChrPostX = contentBox.Left - _drawOffset;
            foreach (var rune in _text.EnumerateRunes())
            {
                if (!font.TryGetCharMetrics(rune, UIScale, out var metrics))
                {
                    index += rune.Utf16SequenceLength;
                    continue;
                }

                if (chrPosX > clickPosX)
                {
                    break;
                }

                lastChrPostX = chrPosX;
                chrPosX += metrics.Advance;
                index += rune.Utf16SequenceLength;

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

                if (char.IsLowSurrogate(_text[index]))
                    index -= 1;
            }

            return index;
        }

        /// <summary>
        /// Get offset from the left of the control
        /// to the left edge of the text glyph at the specified index in the text.
        /// </summary>
        /// <remarks>
        /// The returned value can be outside the bounds of the control if the glyph is currently clipped off.
        /// </remarks>
        public float GetOffsetAtIndex(int index)
        {
            var style = _getStyleBox();
            var contentBox = style.GetContentBox(PixelSizeBox, UIScale);

            var font = _getFont();
            var i = 0;
            var chrPosX = contentBox.Left - _drawOffset;
            foreach (var rune in _text.EnumerateRunes())
            {
                if (i >= index)
                    break;

                if (font.TryGetCharMetrics(rune, UIScale, out var metrics))
                    chrPosX += metrics.Advance;

                i += rune.Utf16SequenceLength;
            }

            return chrPosX / UIScale;
        }

        protected internal override void KeyboardFocusEntered()
        {
            base.KeyboardFocusEntered();

            // Reset this so the cursor is always visible immediately after gaining focus..
            _blink.Reset();
            OnFocusEnter?.Invoke(new LineEditEventArgs(this, _text));

            if (Editable)
            {
                Root?.Window?.TextInputStart();
            }

            _focusedOnFrame = _timing.CurFrame;
            if (SelectAllOnFocus)
            {
                CursorPosition = _text.Length;
                SelectionStart = 0;
            }
        }

        protected internal override void KeyboardFocusExited()
        {
            base.KeyboardFocusExited();

            OnFocusExit?.Invoke(new LineEditEventArgs(this, _text));

            Root?.Window?.TextInputStop();

            AbortIme(delete: false);
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
            if (StyleBoxOverride != null)
            {
                return StyleBoxOverride;
            }

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

            _getStyleBox().Draw(handle, PixelSizeBox, UIScale);
        }

        public sealed class LineEditEventArgs : EventArgs
        {
            public LineEdit Control { get; }
            public string Text { get; }

            public LineEditEventArgs(LineEdit control, string text)
            {
                Control = control;
                Text = text;
            }
        }

        public sealed class LineEditTextRemovedEventArgs : EventArgs
        {
            public string OldText { get; }
            public string NewText { get; }
            public int OldCursorPosition { get; }
            public int OldSelectionStart { get; }

            public LineEditTextRemovedEventArgs(
                string oldText,
                string newText,
                int oldCursorPosition,
                int oldSelectionStart)
            {
                OldText = oldText;
                NewText = newText;
                OldCursorPosition = oldCursorPosition;
                OldSelectionStart = oldSelectionStart;
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

                var uiScale = UIScale;
                var offsetY = (contentBox.Height - font.GetHeight(uiScale)) / 2;

                var renderedText = _master.IsPlaceHolderVisible ? _master._placeHolder! : _master._text;
                DebugTools.AssertNotNull(renderedText);

                ref var drawOffset = ref _master._drawOffset;

                // Go through the entire text once to find length/positional data of cursor.
                var count = 0;
                var posX = 0;
                var actualCursorPosition = 0;
                var actualSelectionStartPosition = 0;

                var actualImeStartPosition = 0;
                var actualImeEndPosition = 0;

                var imeStartIndex = -1;
                var imeEndIndex = -1;

                if (_master._imeData.HasValue)
                {
                    (imeStartIndex, var length) = _master._imeData.Value;
                    imeEndIndex = imeStartIndex + length;
                }

                foreach (var chr in renderedText.EnumerateRunes())
                {
                    if (!font.TryGetCharMetrics(chr, uiScale, out var metrics))
                    {
                        count += 1;
                        continue;
                    }

                    posX += metrics.Advance;
                    count += chr.Utf16SequenceLength;

                    // NOTE: Due to the way this code works, these if statements don't get triggered
                    // if the relevant positions are all the way at the left of the LineEdit.
                    // This happens to be fine, because in that case the horizontal position = 0,
                    // and that's what we initialize the variables to by default.
                    if (count == _master._cursorPosition)
                    {
                        actualCursorPosition = posX;
                    }

                    if (count == _master._selectionStart)
                    {
                        actualSelectionStartPosition = posX;
                    }

                    if (count == imeStartIndex)
                    {
                        actualImeStartPosition = posX;
                    }

                    if (count == imeEndIndex)
                    {
                        actualImeEndPosition = posX;
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
                var baseLine = (-drawOffset, offsetY + font.GetAscent(uiScale)) +
                               contentBox.TopLeft;

                foreach (var rune in renderedText.EnumerateRunes())
                {
                    if (!font.TryGetCharMetrics(rune, uiScale, out var metrics))
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
                        font.DrawChar(handle, rune, baseLine, uiScale, renderedTextColor);
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
                        var color = _master.StylePropertyDefault(
                            StylePropertySelectionColor,
                            Color.CornflowerBlue.WithAlpha(0.25f));

                        handle.DrawRect(
                            new UIBox2(selectionLower, contentBox.Top, selectionUpper, contentBox.Bottom),
                            color);
                    }

                    var cursorColor = _master.StylePropertyDefault(
                        StylePropertyCursorColor,
                        Color.White);

                    cursorColor.A *= _master._blink.Opacity;

                    handle.DrawRect(
                        new UIBox2(actualCursorPosition, contentBox.Top, actualCursorPosition + 1,
                            contentBox.Bottom),
                        cursorColor);

                    if (Root?.Window is { } window)
                    {
                        // Update IME position.
                        var imeBox = new UIBox2(
                            contentBox.Left,
                            contentBox.Top,
                            contentBox.Right,
                            contentBox.Bottom);

                        window.TextInputSetRect((UIBox2i) imeBox.Translated(GlobalPixelPosition), actualCursorPosition);
                    }
                }

                // Draw IME underline if necessary.
                if (_master._imeData.HasValue)
                {
                    var y = baseLine.Y + font.GetDescent(uiScale);
                    var rect = new UIBox2(
                        actualImeStartPosition,
                        y - 1,
                        actualImeEndPosition,
                        y
                    );

                    handle.DrawRect(rect, renderedTextColor);
                }
            }
        }
    }
}
