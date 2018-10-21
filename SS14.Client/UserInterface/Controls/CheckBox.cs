namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.CheckBox))]
    public class CheckBox : Button
    {
        public CheckBox()
        {
        }

        public CheckBox(string name) : base(name)
        {
        }

        internal CheckBox(Godot.CheckBox box) : base(box)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.CheckBox();
        }
    }
}
