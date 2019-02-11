using System;
using SS14.Client.GodotGlue;
using SS14.Client.Graphics.Drawing;
using SS14.Client.Utility;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.LineEdit))]
    public class LineEdit : Control
    {
        private string _text = "";
        private AlignMode _textAlign;
        private bool _editable;
        private string _placeHolder;
        private int _cursorPosition;
        private int? _selectionEnd;

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
            get => GameController.OnGodot ? (AlignMode)SceneControl.Get("align") : _textAlign;
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
            get => GameController.OnGodot ? (string)SceneControl.Get("text") : _text;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Set("text", value);
                }
                else
                {
                    _text = value;
                }
            }
        }

        public bool Editable
        {
            get => GameController.OnGodot ? (bool)SceneControl.Get("editable") : _editable;
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
            get => GameController.OnGodot ? (string)SceneControl.Get("placeholder_text") : _placeHolder;
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
            get => GameController.OnGodot ? (int)SceneControl.Get("caret_position") : default;
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

            var styleBox = UserInterfaceManager.Theme.LineEditBox;
            var drawBox = new UIBox2(Vector2.Zero, Size);
            var contentBox = styleBox.GetContentBox(drawBox);
            styleBox.Draw(handle, drawBox);
            var font = UserInterfaceManager.Theme.DefaultFont;

            var baseLine = new Vector2i(0, (int)(contentBox.Height + font.Ascent)/2) + contentBox.TopLeft;

            if (string.IsNullOrEmpty(_text))
            {
                // Try to draw placeholder.
                if (_placeHolder == null)
                {
                    return;
                }

                foreach (var chr in _placeHolder)
                {
                    var advance = (int) font.DrawChar(handle, chr, baseLine, Color.Red);
                    baseLine += new Vector2(advance, 0);
                }
            }
            else
            {
                foreach (var chr in _text)
                {
                    var advance = (int) font.DrawChar(handle, chr, baseLine, Color.White);
                    baseLine += new Vector2(advance, 0);
                }
            }
        }

        protected override Vector2 CalculateMinimumSize()
        {
            if (GameController.OnGodot)
            {
                return Vector2.Zero;
            }

            var font = UserInterfaceManager.Theme.DefaultFont;
            return new Vector2(0, font.Height) + UserInterfaceManager.Theme.LineEditBox.MinimumSize;
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
