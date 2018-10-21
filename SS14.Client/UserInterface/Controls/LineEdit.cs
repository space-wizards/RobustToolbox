using System;
using SS14.Client.GodotGlue;

namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.LineEdit))]
    public class LineEdit : Control
    {
        public LineEdit() : base()
        {
        }

        public LineEdit(string name) : base(name)
        {
        }

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
            SceneControl = (Godot.LineEdit) control;
        }

        public AlignMode TextAlign
        {
            get => GameController.OnGodot ? (AlignMode) SceneControl.Align : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Align = (Godot.LineEdit.AlignEnum) value;
                }
            }
        }

        public string Text
        {
            get => GameController.OnGodot ? SceneControl.Text : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Text = value;
                }
            }
        }

        public bool Editable
        {
            get => GameController.OnGodot ? SceneControl.Editable : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.Editable = value;
                }
            }
        }

        public string PlaceHolder
        {
            get => GameController.OnGodot ? SceneControl.PlaceholderText : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.PlaceholderText = value;
                }
            }
        }

        // TODO:
        // I decided to not implement the entire LineEdit API yet,
        // since most of it won't be used yet (if at all).
        // Feel free to implement wrappers for all the other properties!

        public void AppendAtCursor(string text)
        {
            if (GameController.OnGodot)
            {
                SceneControl.AppendAtCursor(text);
            }
        }

        public void Clear()
        {
            if (GameController.OnGodot)
            {
                SceneControl.Clear();
            }
        }

        public int CursorPosition
        {
            get => GameController.OnGodot ? SceneControl.GetCursorPosition() : default;
            set
            {
                if (GameController.OnGodot)
                {
                    SceneControl.SetCursorPosition(value);
                }
            }
        }

        public void ExecuteMenuOption(MenuOption option)
        {
            if (GameController.OnGodot)
            {
                SceneControl.MenuOption((int) option);
            }
        }

        public void Select(int from = 0, int to = -1)
        {
            if (GameController.OnGodot)
            {
                SceneControl.Select(from, to);
            }
        }

        public void SelectAll()
        {
            if (GameController.OnGodot)
            {
                SceneControl.SelectAll();
            }
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
    }
}
