namespace SS14.Client.UserInterface
{
    public class WindowDialog : Popup
    {
        public WindowDialog() : base() {}
        public WindowDialog(string name) : base(name) {}
        public WindowDialog(Godot.WindowDialog control) : base(control) {}

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.WindowDialog();
        }
    }
}
