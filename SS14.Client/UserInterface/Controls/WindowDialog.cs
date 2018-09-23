namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.WindowDialog))]
    public class WindowDialog : Popup
    {
        public WindowDialog() : base()
        {
        }
        public WindowDialog(string name) : base(name)
        {
        }
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

        public string Title
        {
            get => SceneControl.WindowTitle;
            set => SceneControl.WindowTitle = value;
        }

        public bool Resizable
        {
            get => SceneControl.Resizable;
            set => SceneControl.Resizable = value;
        }
    }
}
