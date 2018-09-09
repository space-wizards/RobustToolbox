namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.Container))]
    public class Container : Control
    {
        public Container() : base() {}
        public Container(string name) : base(name) {}
        public Container(Godot.Container sceneControl) : base(sceneControl) {}

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Container();
        }
    }
}
