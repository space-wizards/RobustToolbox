namespace Robust.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.VBoxContainer))]
    public class VBoxContainer : BoxContainer
    {
        public VBoxContainer() : base()
        {
        }

        public VBoxContainer(string name) : base(name)
        {
        }

        internal VBoxContainer(Godot.VBoxContainer sceneControl) : base(sceneControl)
        {
        }

        private protected override Godot.Control SpawnSceneControl()
        {
            return new Godot.VBoxContainer();
        }

        private protected override bool Vertical => true;
    }
}
