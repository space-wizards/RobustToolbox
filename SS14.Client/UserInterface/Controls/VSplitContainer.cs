namespace SS14.Client.UserInterface.Controls
{
    [ControlWrap(typeof(Godot.VSplitContainer))]
    public class VSplitContainer : SplitContainer
    {
        private protected sealed override bool Vertical => true;

        public VSplitContainer() {}
        public VSplitContainer(Godot.VSplitContainer control) : base(control) {}
    }
}
