using System;
using SS14.Client.GodotGlue;

namespace SS14.Client.UserInterface.Controls
{
    public class LineEdit : Control
    {
        public LineEdit() : base()
        {
        }
        public LineEdit(string name) : base(name)
        {
        }
        public LineEdit(Godot.LineEdit control) : base(control)
        {
        }

        new private Godot.LineEdit SceneControl;

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.LineEdit();
        }

        protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.LineEdit)control;
        }

        public AlignMode TextAlign
        {
            get => (AlignMode)SceneControl.Align;
            set => SceneControl.Align = (Godot.LineEdit.AlignEnum)value;
        }

        public string Text
        {
            get => SceneControl.Text;
            set => SceneControl.Text = value;
        }

        // TODO:
        // I decided to not implement the entire LineEdit API yet,
        // since most of it won't be used yet (if at all).
        // Feel free to implement wrappers for all the other properties!

        public void AppendAtCursor(string text)
        {
            SceneControl.AppendAtCursor(text);
        }

        public void Clear()
        {
            SceneControl.Clear();
        }

        public int CursorPosition
        {
            get => SceneControl.GetCursorPosition();
            set => SceneControl.SetCursorPosition(value);
        }

        public void ExecuteMenuOption(MenuOption option)
        {
            SceneControl.MenuOption((int)option);
        }

        public void Select(int from = 0, int to = -1)
        {
            SceneControl.Select(from, to);
        }

        public void SelectAll()
        {
            SceneControl.SelectAll();
        }

        public event Action<LineEditEventArgs> OnTextChanged;
        public event Action<LineEditEventArgs> OnTextEntered;

        public enum AlignMode
        {
            Left = Godot.LineEdit.AlignEnum.Left,
            Center = Godot.LineEdit.AlignEnum.Center,
            Right = Godot.LineEdit.AlignEnum.Right,
            Fill = Godot.LineEdit.AlignEnum.Fill,
        }

        public enum MenuOption
        {
            Cut = Godot.LineEdit.MenuItems.Cut,
            Copy = Godot.LineEdit.MenuItems.Copy,
            Paste = Godot.LineEdit.MenuItems.Paste,
            Clear = Godot.LineEdit.MenuItems.Clear,
            SelectAll = Godot.LineEdit.MenuItems.SelectAll,
            Undo = Godot.LineEdit.MenuItems.Undo,
            Redo = Godot.LineEdit.MenuItems.Redo,
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

            __textChangedSubscriber.Disconnect(SceneControl, "text_changed");
            __textChangedSubscriber.Dispose();
            __textChangedSubscriber = null;

            __textEnteredSubscriber.Disconnect(SceneControl, "text_entered");
            __textEnteredSubscriber.Dispose();
            __textEnteredSubscriber = null;
        }

        private void __textChangedHook(object text)
        {
            OnTextChanged?.Invoke(new LineEditEventArgs(this, (string)text));
        }

        private void __textEnteredHook(object text)
        {
            OnTextEntered?.Invoke(new LineEditEventArgs(this, (string)text));
        }
    }
}
