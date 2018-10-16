namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap("MarginContainer")]
    public class MarginContainer : Container
    {
        public MarginContainer()
        {
        }

        public MarginContainer(string name) : base(name)
        {
        }

        #if GODOT
        internal MarginContainer(Godot.MarginContainer sceneControl) : base(sceneControl)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.MarginContainer();
        }
        #endif
    }
}
