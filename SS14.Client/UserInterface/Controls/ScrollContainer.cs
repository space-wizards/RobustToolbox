namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.ScrollContainer))]
    public class ScrollContainer : Container
    {
        public ScrollContainer()
        {
        }

        public ScrollContainer(string name) : base(name)
        {
        }

        public ScrollContainer(Godot.ScrollContainer container) : base(container)
        {

        }

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.ScrollContainer();
        }
    }
}
