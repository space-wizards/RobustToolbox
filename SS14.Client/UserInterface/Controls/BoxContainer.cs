namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.BoxContainer))]
    public class BoxContainer : Control
    {
        public BoxContainer() : base() {}
        public BoxContainer(string name) : base(name) {}
        public BoxContainer(Godot.BoxContainer sceneControl) : base(sceneControl) {}
    }
}
