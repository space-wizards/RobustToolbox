namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.HBoxContainer))]
    public class HBoxContainer : BoxContainer
    {
        public HBoxContainer() : base()
        {
        }

        public HBoxContainer(string name) : base(name)
        {
        }

        internal HBoxContainer(Godot.HBoxContainer sceneControl) : base(sceneControl)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.HBoxContainer();
        }

        private protected override bool Vertical => false;
    }
}
