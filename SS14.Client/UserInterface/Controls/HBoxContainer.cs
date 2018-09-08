namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.HBoxContainer))]
    public class HBoxContainer : BoxContainer
    {
        public HBoxContainer() : base() { }
        public HBoxContainer(string name) : base(name) { }
        public HBoxContainer(Godot.HBoxContainer sceneControl) : base(sceneControl) { }

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.HBoxContainer();
        }
    }
}
