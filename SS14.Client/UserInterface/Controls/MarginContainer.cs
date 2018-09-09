namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.MarginContainer))]
    public class MarginContainer : Container
    {
        public MarginContainer()
        {
        }

        public MarginContainer(string name) : base(name)
        {
        }

        public MarginContainer(Godot.MarginContainer sceneControl) : base(sceneControl)
        {
        }

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.MarginContainer();
        }
    }
}
