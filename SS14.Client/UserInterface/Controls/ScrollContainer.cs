namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap("ScrollContainer")]
    public class ScrollContainer : Container
    {
        public ScrollContainer()
        {
        }

        public ScrollContainer(string name) : base(name)
        {
        }

        #if GODOT
        internal ScrollContainer(Godot.ScrollContainer container) : base(container)
        {

        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.ScrollContainer();
        }
        #endif
    }
}
