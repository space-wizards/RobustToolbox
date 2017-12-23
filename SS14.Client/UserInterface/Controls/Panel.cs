namespace SS14.Client.UserInterface
{
    public class Panel : Control
    {
        public Panel(string name) : base(name) {}
        public Panel() : base() {}

        protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.Panel();
        }
    }
}
