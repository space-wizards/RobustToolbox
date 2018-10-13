using System;

namespace SS14.Client.UserInterface.Controls
{
    #if GODOT
    [ControlWrap(typeof(Godot.WindowDialog))]
    #endif
    public class WindowDialog : Popup
    {
        public WindowDialog() : base()
        {
        }
        public WindowDialog(string name) : base(name)
        {
        }
        #if GODOT
        internal WindowDialog(Godot.WindowDialog control) : base(control)
        {
        }

        new private Godot.WindowDialog SceneControl;

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.WindowDialog();
        }

        private protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.WindowDialog)control;
        }
        #endif

        public string Title
        {
            #if GODOT
            get => SceneControl.WindowTitle;
            set => SceneControl.WindowTitle = value;
            #else
            get => default;
            set { }
            #endif
        }

        public bool Resizable
        {
            #if GODOT
            get => SceneControl.Resizable;
            set => SceneControl.Resizable = value;
            #else
            get => default;
            set { }
            #endif
        }
    }
}
