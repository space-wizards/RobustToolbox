namespace SS14.Client.UserInterface
{
    /// <summary>
    ///     A label is a GUI control that displays simple text.
    /// </summary>
    public class Label : Control
    {
        public Label(string name) : base(name) {}
        public Label() : base() {}

        public string Text
        {
            get => SceneControl.Text;
            set => SceneControl.Text = value;
        }

        new private Godot.Label SceneControl;

        protected override Godot.Control SpawnSceneControl()
        {
            SceneControl = new Godot.Label();
            return SceneControl;
        }
    }
}
