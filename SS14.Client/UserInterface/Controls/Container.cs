namespace SS14.Client.UserInterface.Controls
{
    #if GODOT
    [ControlWrap(typeof(Godot.Container))]
    #endif
    public class Container : Control
    {
        public Container() : base() {}
        public Container(string name) : base(name) {}
        #if GODOT
        internal Container(Godot.Container sceneControl) : base(sceneControl) {}

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Container();
        }
        #endif
    }
}
