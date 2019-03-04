using System;
using JetBrains.Annotations;
using SS14.Client.GodotGlue;
using SS14.Client.Graphics;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Input;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.LineEdit))]
    public class LineEdit : Control
    {
        public const string StylePropertyStyleBox = "stylebox";

        [NotNull] private string _text = "";
        private AlignMode _textAlign;
        private bool _editable;
        [CanBeNull] private string _placeHolder;
        private int _cursorPosition;
        private float _cursorBlinkTimer;
        private bool _cursorCurrentlyLit;
        private const float BlinkTime = 0.5f;

        public LineEdit()
        {
        }

        public LineEdit(string name) : base(name)
        {
        }

        internal LineEdit(Godot.LineEdit control) : base(control)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.LineEdit();
        }

        public AlignMode TextAlign
        {
            get => GameController.OnGodot ? (AlignMode) SceneControl.Get("align") : _textAlign;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("align", (Godot.LineEdit.AlignEnum) value);
                }
                else
                {
                    _textAlign = value;
                }
            }
        }

        public string Text
        {
            get => GameController.OnGodot ? (string) SceneControl.Get("text") : _text;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("text", value);
                }
                else
                {
                    if (value == null)
                    {
                        value = "";
                    }

                    _text = value;
                    _cursorPosition = 0;
                }
            }
        }

        public bool Editable
        {
            get => GameController.OnGodot ? (bool) SceneControl.Get("editable") : _editable;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("editable", value);
                }
                else
                {
                    _editable = value;
                }
            }
        }

        public string PlaceHolder
        {
            get => GameController.OnGodot ? (string) SceneControl.Get("placeholder_text") : _placeHolder;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("placeholder_text", value);
                }
                else
                {
                    _placeHolder = value;
                }
            }
        }

        public bool IgnoreNext { get; set; }

        // TODO:
        // I decided to not implement the entire LineEdit API yet,
        // since most of it won't be used yet (if at all).
        // Feel free to implement wrappers for all the other properties!
        // Future me reporting, thanks past me.
        // Second future me reporting, thanks again.
        // Third future me is here to say thanks.

        public void AppendAtCursor(string text)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("append_at_cursor", text);
            }
        }

        public void Clear()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("clear");
            }

            Text = "";
        }

        public int CursorPosition
        {
            get => GameController.OnGodot ? (int) SceneControl.Get("caret_position") : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("caret_position", value);
                }
            }
        }

        public void Select(int from = 0, int to = -1)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("select", from, to);
            }
        }

        public void SelectAll()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Call("select_all");
            }
        }

        public event Action<LineEditEventArgs> OnTextChanged;
        public event Action<LineEditEventArgs> OnTextEntered;

        protected internal override void Draw(DrawingHandleScreen handle)
        {
            if (GameController.OnGodot)
            {
                return;
            }

            var styleBox = _getStyleBox();
            var drawBox = SizeBox;
            var contentBox = styleBox.GetContentBox(drawBox);
            styleBox.Draw(handle, drawBox);
            var font = _getFont();

            var offsetY = (int) (contentBox.Height - font.Height) / 2;
            var baseLine = new Vector2i(0, offsetY+font.Ascent) + contentBox.TopLeft;

            string renderedText;
            Color renderedTextColor;

            if (string.IsNullOrEmpty(_text) && _placeHolder != null)
            {
                renderedText = _placeHolder;
                renderedTextColor = Color.Gray;
            }
            else
            {
                renderedText = _text;
                renderedTextColor = Color.White;
            }

            float? actualCursorPosition = null;

            if (_cursorPosition == 0)
            {
                actualCursorPosition = contentBox.Left;
            }

            var count = 0;
            foreach (var chr in renderedText)
            {
                if (!font.TryGetCharMetrics(chr, out var metrics))
                {
                    count += 1;
                    continue;
                }

                // Glyph would be outside the bounding box, abort.
                if (baseLine.X + metrics.Width + metrics.BearingX > contentBox.Right)
                {
                    break;
                }

                font.DrawChar(handle, chr, baseLine, renderedTextColor);
                baseLine += new Vector2(metrics.Advance, 0);
                count += 1;
                if (count == _cursorPosition)
                {
                    actualCursorPosition = baseLine.X;
                }
            }

            if (_cursorCurrentlyLit && actualCursorPosition.HasValue && HasKeyboardFocus())
            {
                handle.DrawRect(
                    new UIBox2(actualCursorPosition.Value, contentBox.Top, actualCursorPosition.Value + 1,
                        contentBox.Bottom), Color.White);
            }
        }

        protected override void FrameUpdate(RenderFrameEventArgs args)
        {
            base.FrameUpdate(args);

            if (GameController.OnGodot)
            {
                return;
            }

            _cursorBlinkTimer -= args.Elapsed;
            if (_cursorBlinkTimer <= 0)
            {
                _cursorBlinkTimer += BlinkTime;
                _cursorCurrentlyLit = !_cursorCurrentlyLit;
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot)
            {
                return Vector2.Zero;
            }

            var font = _getFont();
            var style = _getStyleBox();
            return new Vector2(0, font.Height) + style.MinimumSize;
        }

        protected internal override void TextEntered(GUITextEventArgs args)
        {
            base.TextEntered(args);

            if (GameController.OnGodot)
            {
                return;
            }

            if (IgnoreNext)
            {
                IgnoreNext = false;
                return;
            }

            _text = _text.Insert(_cursorPosition, ((char) args.CodePoint).ToString());
            OnTextChanged?.Invoke(new LineEditEventArgs(this, _text));
            _cursorPosition += 1;
        }

        protected internal override void KeyDown(GUIKeyEventArgs args)
        {
            base.KeyDown(args);

            if (GameController.OnGodot)
            {
                return;
            }

            // Just eat all keyboard input.
            args.Handle();

            switch (args.Key)
            {
                case Keyboard.Key.BackSpace:
                    if (_cursorPosition == 0)
                    {
                        return;
                    }

                    _text = _text.Remove(_cursorPosition - 1, 1);
                    OnTextChanged?.Invoke(new LineEditEventArgs(this, _text));
                    _cursorPosition -= 1;
                    break;

                case Keyboard.Key.Left:
                    if (_cursorPosition == 0)
                    {
                        return;
                    }

                    _cursorPosition -= 1;
                    break;

                case Keyboard.Key.Right:
                    if (_cursorPosition == _text.Length)
                    {
                        return;
                    }

                    _cursorPosition += 1;
                    break;

                case Keyboard.Key.NumpadEnter:
                case Keyboard.Key.Return:
                    OnTextEntered?.Invoke(new LineEditEventArgs(this, _text));
                    break;
            }
        }

        protected internal override void MouseDown(GUIMouseButtonEventArgs args)
        {
            base.MouseDown(args);

            if (GameController.OnGodot)
            {
                return;
            }

            // Find closest cursor position under mouse.
            var style = _getStyleBox();
            var contentBox = style.GetContentBox(SizeBox);

            var clickPosX = args.RelativePosition.X;

            var font = _getFont();
            var index = 0;
            var chrPosX = contentBox.Left;
            var lastChrPostX = contentBox.Left;
            foreach (var chr in _text)
            {
                if (!font.TryGetCharMetrics(chr, out var metrics))
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
            if (distanceRight > distanceLeft)
            {
                index -= 1;
            }

            _cursorPosition = index;

            // Reset this so the cursor is always visible immediately after a click.
            _cursorCurrentlyLit = true;
            _cursorBlinkTimer = BlinkTime;
        }

        protected internal override void FocusEntered()
        {
            base.FocusEntered();


            if (GameController.OnGodot)
            {
                return;
            }

            // Reset this so the cursor is always visible immediately after gaining focus..
            _cursorCurrentlyLit = true;
            _cursorBlinkTimer = BlinkTime;
        }

        protected override void SetDefaults()
        {
            base.SetDefaults();

            MouseFilter = MouseFilterMode.Stop;
            CanKeyboardFocus = true;
            KeyboardFocusOnClick = true;
        }

        [Pure]
        private Font _getFont()
        {
            if (TryGetStyleProperty("font", out Font font))
            {
                return font;
            }

            return UserInterfaceManager.ThemeDefaults.DefaultFont;
        }

        [Pure]
        private StyleBox _getStyleBox()
        {
            if (TryGetStyleProperty(StylePropertyStyleBox, out StyleBox box))
            {
                return box;
            }

            return UserInterfaceManager.ThemeDefaults.LineEditBox;
        }

        public enum AlignMode
        {
            Left = 0,
            Center = 1,
            Right = 2,
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

        private GodotSignalSubscriber1 __textChangedSubscriber;
        private GodotSignalSubscriber1 __textEnteredSubscriber;

        protected override void SetupSignalHooks()
        {
            base.SetupSignalHooks();

            __textChangedSubscriber = new GodotSignalSubscriber1();
            __textChangedSubscriber.Connect(SceneControl, "text_changed");
            __textChangedSubscriber.Signal += __textChangedHook;

            __textEnteredSubscriber = new GodotSignalSubscriber1();
            __textEnteredSubscriber.Connect(SceneControl, "text_entered");
            __textEnteredSubscriber.Signal += __textEnteredHook;
        }

        protected override void DisposeSignalHooks()
        {
            base.DisposeSignalHooks();

            if (__textChangedSubscriber != null)
            {
                __textChangedSubscriber.Disconnect(SceneControl, "text_changed");
                __textChangedSubscriber.Dispose();
                __textChangedSubscriber = null;
            }

            if (__textEnteredSubscriber != null)
            {
                __textEnteredSubscriber.Disconnect(SceneControl, "text_entered");
                __textEnteredSubscriber.Dispose();
                __textEnteredSubscriber = null;
            }
        }

        private void __textChangedHook(object text)
        {
            OnTextChanged?.Invoke(new LineEditEventArgs(this, (string) text));
        }

        private void __textEnteredHook(object text)
        {
            OnTextEntered?.Invoke(new LineEditEventArgs(this, (string) text));
        }

        private protected override void SetGodotProperty(string property, object value, GodotAssetScene context)
        {
            base.SetGodotProperty(property, value, context);

            if (property == "text")
            {
                Text = (string) value;
            }
            else if (property == "placeholder_text")
            {
                PlaceHolder = (string) value;
            }
            else if (property == "editable")
            {
                Editable = (bool) value;
            }
        }
    }
}
