namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.VBoxContainer))]
    public class VBoxContainer : BoxContainer
    {
        public VBoxContainer() : base() { }
        public VBoxContainer(string name) : base(name) { }
        public VBoxContainer(Godot.VBoxContainer sceneControl) : base(sceneControl) { }

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.VBoxContainer();
        }
    }
}
