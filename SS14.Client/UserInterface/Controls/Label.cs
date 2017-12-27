using Godot;

namespace SS14.Client.UserInterface
{
    /// <summary>
    ///     A label is a GUI control that displays simple text.
    /// </summary>
    public class Label : Control
    {
        public Label(string name) : base(name)
        {
        }
        public Label() : base()
        {
        }
        public Label(Godot.Label control) : base(control)
        {
        }

        public string Text
        {
            get => SceneControl.Text;
            set => SceneControl.Text = value;
        }

        public bool AutoWrap
        {
            get => SceneControl.Autowrap;
            set => SceneControl.Autowrap = value;
        }

        new private Godot.Label SceneControl;

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Label();
        }

        protected override void SetSceneControl(Godot.Control control)
        {
            base.SetSceneControl(control);
            SceneControl = (Godot.Label)control;
        }
    }
}
