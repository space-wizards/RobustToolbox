namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap("CheckBox")]
    public class CheckBox : Button
    {
        public CheckBox()
        {
        }

        public CheckBox(string name) : base(name)
        {
        }

        #if GODOT
        internal CheckBox(Godot.CheckBox box) : base(box)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.CheckBox();
        }
        #endif
    }
}
