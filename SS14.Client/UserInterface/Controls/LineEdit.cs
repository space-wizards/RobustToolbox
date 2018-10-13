using System;
#if GODOT
using SS14.Client.GodotGlue;
#endif

namespace SS14.Client.UserInterface.Controls
{
    #if GODOT
    [ControlWrap(typeof(Godot.LineEdit))]
    #endif
    public class LineEdit : Control
    {
        public LineEdit() : base()
        {
        }
        public LineEdit(string name) : base(name)
        {
        }

        #if GODOT
        internal LineEdit(Godot.LineEdit control) : base(control)
        {
        }

        new private Godot.LineEdit SceneControl;

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.LineEdit();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.LineEdit)control;
        }
        #endif

        public AlignMode TextAlign
        {
            #if GODOT
            get => (AlignMode)SceneControl.Align;
            set => SceneControl.Align = (Godot.LineEdit.AlignEnum)value;
            #else
            get => default;
            set { }
            #endif
        }

        public string Text
        {
            #if GODOT
            get => SceneControl.Text;
            set => SceneControl.Text = value;
            #else
            get => default;
            set { }
            #endif
        }

        public bool Editable
        {
            #if GODOT
            get => SceneControl.Editable;
            set => SceneControl.Editable = value;
            #else
            get => default;
            set { }
            #endif
        }

        public string PlaceHolder
        {
            #if GODOT
            get => SceneControl.PlaceholderText;
            set => SceneControl.PlaceholderText = value;
            #else
            get => default;
            set { }
            #endif
        }

        // TODO:
        // I decided to not implement the entire LineEdit API yet,
        // since most of it won't be used yet (if at all).
        // Feel free to implement wrappers for all the other properties!

        public void AppendAtCursor(string text)
        {
            #if GODOT
            SceneControl.AppendAtCursor(text);
            #endif
        }

        public void Clear()
        {
            #if GODOT
            SceneControl.Clear();
            #endif
        }

        public int CursorPosition
        {
            #if GODOT
            get => SceneControl.GetCursorPosition();
            set => SceneControl.SetCursorPosition(value);
            #else
            get => default;
            set { }
            #endif
        }

        public void ExecuteMenuOption(MenuOption option)
        {
            #if GODOT
            SceneControl.MenuOption((int)option);
            #endif
        }

        public void Select(int from = 0, int to = -1)
        {
            #if GODOT
            SceneControl.Select(from, to);
            #endif
        }

        public void SelectAll()
        {
            #if GODOT
            SceneControl.SelectAll();
            #endif
        }

        public event Action<LineEditEventArgs> OnTextChanged;
        public event Action<LineEditEventArgs> OnTextEntered;

        public enum AlignMode
        {
            Left = 0,
            Center = 1,
            Right = 2,
            Fill = 3,
        }

        public enum MenuOption
        {
            Cut = 0,
            Copy = 1,
            Paste = 2,
            Clear = 3,
            SelectAll = 4,
            Undo = 5,
            Redo = 6,
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

        #if GODOT
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
            OnTextChanged?.Invoke(new LineEditEventArgs(this, (string)text));
        }

        private void __textEnteredHook(object text)
        {
            OnTextEntered?.Invoke(new LineEditEventArgs(this, (string)text));
        }
        #endif
    }
}
